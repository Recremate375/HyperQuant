using HyperQuant.Core.Interfaces;
using HyperQuant.Core.Models;

namespace HyperQuant.Connector.Services
{
    public class ClientConnector : IRestService, IWebSocketService, IDisposable
    {
        private readonly RestService _restService;
        private readonly WebSocketService _webSocketService;

        public ClientConnector()
        {
            _restService = new();
            _webSocketService = new();
        }

        public event Action<Trade> NewBuyTrade
        {
            add => _webSocketService.NewBuyTrade += value;
            remove => _webSocketService.NewBuyTrade -= value;
        }
        public event Action<Trade> NewSellTrade
        {
            add => _webSocketService.NewSellTrade += value;
            remove => _webSocketService.NewSellTrade -= value;
        }
        public event Action<Candle> CandleSeriesProcessing
        {
            add => _webSocketService.CandleSeriesProcessing += value;
            remove => _webSocketService.CandleSeriesProcessing -= value;
        }
        public void Dispose()
        {
            _restService.Dispose();
            _webSocketService.Dispose();
        }

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            return await _restService.GetCandleSeriesAsync(pair, periodInSec, from, to, count);
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            return await _restService.GetNewTradesAsync(pair, maxCount);
        }

        public async Task SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            await _webSocketService.SubscribeCandles(pair, periodInSec, from, to, count);
        }

        public async Task SubscribeTrades(string pair, int maxCount = 100)
        {
            await _webSocketService.SubscribeTrades(pair, maxCount);
        }

        public void UnsubscribeCandles(string pair)
        {
            _webSocketService.UnsubscribeCandles(pair);
        }

        public void UnsubscribeTrades(string pair)
        {
            _webSocketService.UnsubscribeTrades(pair);
        }
    }
}
