using HyperQuant.Core.Interfaces;
using HyperQuant.Connector.Parsers;
using HyperQuant.Core.Models;
using System;
using System.Text.Json;

namespace HyperQuant.Connector.Services
{
    public class RestService : IRestService
    {
        private const string BASE_URI = "https://api-pub.bitfinex.com/v2/";
        private readonly HttpClient _httpClient;
        private readonly ApiResponseParser _responseParser;

        public RestService()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(BASE_URI)
            };

            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _responseParser = new();
        }

        public void Dispose() => _httpClient.Dispose();

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            var periodInBitfinex = TimeConverterForCandles.ConvertSecondsToBitfinexTime(periodInSec);
            var url =  $"candles/trade:{periodInBitfinex}:{pair}/hist";

            var parameters = new List<string>();
            if (from.HasValue) parameters.Add($"start={from.Value.ToUnixTimeMilliseconds()}");
            if (to.HasValue) parameters.Add($"end={to.Value.ToUnixTimeMilliseconds()}");
            if (count.HasValue) parameters.Add($"limit={count}");

            if (parameters.Count != 0) url += "?" + string.Join("&", parameters);

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var elements = JsonDocument.Parse(content).RootElement;

            return _responseParser.ParseCandles(elements, pair);
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            var url = $"trades/{pair}/hist?limit={maxCount}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var elements = JsonDocument.Parse(content).RootElement;

            return _responseParser.ParseTrades(elements, pair);
        }
    }
}
