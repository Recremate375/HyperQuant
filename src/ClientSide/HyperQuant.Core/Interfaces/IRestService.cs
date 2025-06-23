using HyperQuant.Core.Models;

namespace HyperQuant.Core.Interfaces
{
    public interface IRestService : IDisposable
    {
        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);
    }
}
