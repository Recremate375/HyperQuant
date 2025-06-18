using HyperQuant.Core.Model;
using System.Text.Json;

namespace HyoerQuant.Core.Interfaces
{
    public interface IApiResponseParser
    {
        IEnumerable<Trade> ParseTrades(JsonElement element, string pair);

        IEnumerable<Candle> ParseCandles(JsonElement element, string pair);
    }
}
