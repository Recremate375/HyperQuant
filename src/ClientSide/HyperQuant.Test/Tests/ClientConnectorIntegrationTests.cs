using HyperQuant.Connector.Services;
using HyperQuant.Core.Models;
using System.Net.WebSockets;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Assert = Xunit.Assert;

namespace HyperQuant.Test.Tests
{
    public class ClientConnectorIntegrationTests : IDisposable
    {
        private readonly ClientConnector _connector;
        private readonly ITestOutputHelper _output;
        private readonly List<Trade> _receivedTrades = new();
        private readonly List<Candle> _receivedCandles = new();
        private readonly AutoResetEvent _tradeEvent = new(false);
        private readonly AutoResetEvent _candleEvent = new(false);
        public ClientConnectorIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
            _connector = new();

            _connector.NewBuyTrade += OnNewTrade;
            _connector.NewSellTrade += OnNewTrade;
            _connector.CandleSeriesProcessing += OnNewCandle;
        }

        private void OnNewTrade(Trade trade)
        {
            _output.WriteLine($"Trade received: {trade.Pair} {trade.Price}");
            _receivedTrades.Add(trade);
            _tradeEvent.Set();
        }

        private void OnNewCandle(Candle candle)
        {
            _output.WriteLine($"Candle received: {candle.Pair} {candle.OpenTime}");
            _receivedCandles.Add(candle);
            _candleEvent.Set();
        }

        [Fact]
        public async Task GetNewTradesAsync()
        {
            //Arrange
            const string pair = "tBTCUSD";
            const int maxCount = 5;

            //Act
            var trades = (await _connector.GetNewTradesAsync(pair, maxCount)).ToList();

            //Assert
            Assert.NotEmpty(trades);
            Assert.InRange(trades.Count, 1, maxCount);
            Assert.All(trades, t =>
            {
                Assert.Equal(pair, t.Pair);
                Assert.True(t.Price > 0);
                Assert.True(t.Amount > 0);
                Assert.NotNull(t.Id);
            });
        }

        [Fact]
        public async Task GetCandlesAsync()
        {
            //Arrange
            const string pair = "tBTCUSD";
            const int period = 3600;
            var from = DateTimeOffset.UtcNow.AddDays(-1);
            var to = DateTimeOffset.UtcNow;

            //Act
            var candles = (await _connector.GetCandleSeriesAsync(pair, period, from, to)).ToList();

            //Assert
            Assert.NotEmpty(candles);
            Assert.All(candles, c =>
            {
                Assert.Equal(pair, c.Pair);
                Assert.True(c.OpenPrice > 0);
                Assert.True(c.HighPrice >= c.LowPrice);
                Assert.True(c.OpenTime < to && c.OpenTime > from);
            });
        }

        [Fact]
        public async Task CanConnectToWebSocketServer()
        {
            using var ws = new ClientWebSocket();
            try
            {
                await ws.ConnectAsync(new Uri("wss://api-pub.bitfinex.com/ws/2"), CancellationToken.None);
                Assert.Equal(WebSocketState.Open, ws.State);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw new Exception($"WebSocket server unavailable: {ex.Message}");
            }
        }

        [Fact]
        public async Task SubscribeTrades()
        {
            //Arrange
            const string pair = "tBTCUSD";
            var timeout = TimeSpan.FromSeconds(60);

            //Act
            await _connector.SubscribeTrades(pair);

            var received = _tradeEvent.WaitOne(timeout);

            //Assert
            Assert.True(received, "No trade events received within timeout");
            Assert.NotEmpty(_receivedTrades);
            Assert.Contains(_receivedTrades, t => t.Pair == pair);
        }

        [Fact]
        public async Task SubscribeCandles()
        {
            //Arrange
            const string pair = "tBTCUSD";
            const int period = 60;
            var timeout = TimeSpan.FromSeconds(60);

            //Act
            await _connector.SubscribeCandles(pair, period);

            var received = _candleEvent.WaitOne(timeout);

            //Assert
            Assert.True(received, "No candle events received within timeout");
            Assert.NotEmpty(_receivedCandles);
            Assert.Contains(_receivedCandles, c => c.Pair == pair);
        }

        [Fact]
        public async Task UnsubscribeTrades()
        {
            //Arrange
            const string pair = "tETHUDSD";
            await _connector.SubscribeTrades(pair);

            _tradeEvent.WaitOne(TimeSpan.FromSeconds(20));
            var initialCount = _receivedTrades.Count;

            //Act
            _connector.UnsubscribeTrades(pair);
            _receivedTrades.Clear();

            await Task.Delay(10000);

            //Assert
            Assert.Equal(initialCount, _receivedTrades.Count);
        }

        [Fact]
        public async Task UnsubscibeCandles()
        {
            //Arrange
            const string pair = "tETHUSD";
            const int period = 60;
            await _connector.SubscribeCandles(pair, period);

            _tradeEvent.WaitOne(TimeSpan.FromSeconds(20));
            var initialCount = _receivedCandles.Count;

            //Act
            _connector.UnsubscribeCandles(pair);
            _receivedCandles.Clear();

            await Task.Delay(10000);

            //Assert
            Assert.Equal(initialCount, _receivedCandles.Count);
        }

        public void Dispose()
        {
            _connector.Dispose();
            _tradeEvent.Dispose();
            _candleEvent.Dispose();
        }
    }
}
