using HyoerQuant.Core.Interfaces;
using HyperQuant.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperQuant.Connector.Services
{
    public class RestService : IRestService
    {
        private const string BASE_URI = "https://api-pub.com.bitfinex.com/v2/";
        private readonly HttpClient _httpClient;
        private readonly _responseParser;

        public RestService()
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(BASE_URI)
            };

            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public void Dispose() => _httpClient.Dispose();

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            try
            {
                var url = $"trades/{pair}/hist?limit={maxCount}";
                var response = await _httpClient.GetAsync(url);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
