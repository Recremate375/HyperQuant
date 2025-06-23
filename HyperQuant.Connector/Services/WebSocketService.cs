using HyperQuant.Core.Interfaces;
using HyperQuant.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HyperQuant.Connector.Services
{
    public class WebSocketService : IWebSocketService
    {
        private ClientWebSocket _webSocket = new();
        private readonly Uri _uri = new("wss://api-pub.bitfinex.com/ws/2");
        private readonly CancellationTokenSource _cts = new();
        private readonly Dictionary<string, int> _channelIds = new();
        private readonly Dictionary<int, Action<JsonElement>> _messageHandlers = new();
        private readonly Dictionary<int, string> _channelIdToPair = new();
        private Task _receiveTask;

        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;

        public WebSocketService()
        {
            _receiveTask = Task.Run(() => InitializeConnectionAsync());
        }
        
        private async Task InitializeConnectionAsync()
        {
            try
            {
                await EnsureConnectedAsync();
                await ReceiveMessageAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initializing error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _webSocket?.Dispose();
        }

        public async Task SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            var timeframe = ConvertPeriodTimeFrame(periodInSec);
            while (_webSocket.State == WebSocketState.Connecting)
            {
                await Task.Delay(100);
            }
            var payload = CreateSubscriptionPayload("candles", $"trade:{timeframe}:{pair}");
            await SendAsync(payload);
        }

        public async Task SubscribeTrades(string pair, int maxCount = 100)
        {
            await EnsureConnectedAsync();
            while (_webSocket.State == WebSocketState.Connecting)
            {
                await Task.Delay(100);
            }

            var payload = CreateSubscriptionPayload("trades", pair);
            await SendAsync(payload);
        }

        public void UnsubscribeCandles(string pair)
        {
            var key = $"candles:{pair}";

            if (_channelIds.TryGetValue(key, out var channelId))
            {
                _channelIds.Remove(key);
                _messageHandlers.Remove(channelId);
            }
        }

        public void UnsubscribeTrades(string pair)
        {
            var key = $"trades:{pair}";
            if (_channelIds.TryGetValue(key, out var channelId))
            {
                _channelIds.Remove(key);
                _messageHandlers.Remove(channelId);
            }
        }

        public async Task EnsureConnectedAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                return;
            }

            try
            {
                if (_webSocket.State != WebSocketState.None && _webSocket.State != WebSocketState.Connecting)
                {
                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                }

                if(_webSocket.State == WebSocketState.None)
                {
                    Console.WriteLine("Connecting to WebSocket...");
                    await _webSocket.ConnectAsync(_uri, _cts.Token);

                    Console.WriteLine($"Connected. State: {_webSocket.State}");

                    if (_receiveTask?.IsCompleted ?? true)
                    {
                        _receiveTask = Task.Run(ReceiveMessageAsync);
                    }
                }

                if (_webSocket.State != WebSocketState.Open)
                {
                    throw new InvalidOperationException($"WebSocket connection failed. State: {_webSocket.State}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task ReceiveMessageAsync()
        {
            var buffer = new byte[2048];

            try
            {
                while (!_cts.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
                {
                    if (_webSocket.State != WebSocketState.Open)
                    {
                        await Task.Delay(1000, _cts.Token);
                        continue;
                    }

                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                            "Closed by server.", CancellationToken.None);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void ProcessMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("event", out var eventProperty))
                {
                    if (eventProperty.ValueEquals("subscribed"))
                    {
                        var channel = root.GetProperty("channel").GetString();
                        var symbol = root.GetProperty("symbol").GetString();
                        var channelId = root.GetProperty("chanId").GetInt32();

                        _channelIds[$"{channel}:{symbol}"] = channelId;
                        _channelIdToPair[channelId] = symbol;
                        RegisterHandler(channelId, channel);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void RegisterHandler(int channelId, string channelType)
        {
            _messageHandlers[channelId] = channelType switch
            {
                "trades" => HandleTradeMessage,
                "candles" => HandleCandleMessage,
                _ => _ => { }
            };
        }

        private void HandleTradeMessage(JsonElement data) 
        {
            if (data[1].ValueEquals("hb"))
            {
                return;
            }

            var channelId = data[0].GetInt32();
            var pair = _channelIdsToPair[channelId];

            if ((data[1].ValueEquals("tu") || data[1].ValueEquals("te")) && data.GetArrayLength() >= 3)
            {
                var tradeData = data[2];
                var trade = ParseTrade(tradeData);
                trade.Pair = 
                if (trade.Side == "buy")
                {
                    NewBuyTrade?.Invoke(trade);
                }
                else
                {
                    NewSellTrade?.Invoke(trade);
                }
            }
        }

        private Trade ParseTrade(JsonElement element)
        {
            return new Trade()
            {
                Id = element[0].GetInt64().ToString(),
                Time = DateTimeOffset.FromUnixTimeMilliseconds(element[1].GetInt64()),
                Amount = Math.Abs(element[2].GetDecimal()),
                Price = element[3].GetDecimal(),
                Side = element[2].GetDecimal() > 0 ? "buy" : "sell"
            };
        }

        private void HandleCandleMessage(JsonElement data)
        {
            if (data.GetArrayLength() < 2)
            {
                return;
            }

            var candleData = data[1];
            if (candleData.ValueKind == JsonValueKind.Array && candleData.GetArrayLength() >= 6)
            {
                var candle = ParseCandle(candleData);
                CandleSeriesProcessing?.Invoke(candle);
            }
        }

        private Candle ParseCandle(JsonElement candleData)
        {
            return new Candle()
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleData[0].GetInt64()),
                OpenPrice = candleData[1].GetDecimal(),
                ClosePrice = candleData[2].GetDecimal(),
                HighPrice = candleData[3].GetDecimal(),
                LowPrice = candleData[4].GetDecimal(),
                TotalVolume = candleData[5].GetDecimal()
            };
        }

        private async Task SendAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text,
                true, _cts.Token);
        }

        private string CreateSubscriptionPayload(string channel, string symbol)
        {
            return $"{{\"event\": \"subscribe\",\"channel\": \"{channel}\", \"symbol\": \"{symbol}\"}}";
        }

        private string ConvertPeriodTimeFrame(int periodInSec)
        {
            return periodInSec switch
            {
                60 => "1m",
                300 => "5m",
                900 => "15m",
                1800 => "30m",
                3600 => "1h",
                10800 => "3h",
                21600 => "6h",
                43200 => "12h",
                86400 => "1D",
                604800 => "7D",
                2592000 => "1M",
                _ => throw new ArgumentException($"Unsupported period: {periodInSec}")
            };
        }
    }
}
