using HyperQuant.Core.Models;
using System.Text.Json;

namespace HyperQuant.Core.Interfaces
{
    public interface IApiResponseParser
    {
        IEnumerable<Trade> ParseTrades(JsonElement element, string pair);

        IEnumerable<Candle> ParseCandles(JsonElement element, string pair);
    }
}
