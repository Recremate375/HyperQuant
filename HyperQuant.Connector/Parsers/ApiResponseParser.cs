using HyoerQuant.Core.Interfaces;
using HyperQuant.Core.Model;
using System.Text.Json;

namespace HyperQuant.Connector.Parsers
{
    public class ApiResponseParser : IApiResponseParser
    {


        public IEnumerable<Candle> ParseCandles(JsonElement element, string pair)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<Candle>();
            }

            return element.EnumerateArray().Select(element => ParseCandle(element, pair)).Where(candle => candle != null);
        }

        private static Candle ParseCandle(JsonElement candleElement, string pair)
        {
            try
            {
                var elements = candleElement.EnumerateArray().ToArray();

                if (elements.Length < 6)
                {
                    return null;
                }

                var candle = new Candle()
                {
                    Pair = pair,
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(elements[0].GetInt64()),
                    OpenPrice = elements[1].GetDecimal(),
                    ClosePrice = elements[2].GetDecimal(),
                    HighPrice = elements[3].GetDecimal(),
                    LowPrice = elements[4].GetDecimal(),
                    TotalVolume = elements[5].GetDecimal(),
                    TotalPrice = 0
                };

                return candle;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse candle data", ex);
            }
        }

        public IEnumerable<Trade> ParseTrades(JsonElement element, string pair)
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<Trade>();
            }

            return element.EnumerateArray().Select(element => ParseTrade(element, pair)).Where(trade => trade != null);

            
        }
        private static Trade ParseTrade(JsonElement tradeElement, string pair)
        {
            try
            {
                var elements = tradeElement.EnumerateArray().ToArray();

                if (elements.Length < 4)
                {
                    return null;
                }

                var trade = new Trade()
                {
                    Id = elements[0].GetInt64().ToString(),
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(elements[1].GetInt64()),
                    Amount = Math.Abs(elements[2].GetDecimal()),
                    Price = elements[3].GetDecimal(),
                    Side = elements[2].GetDecimal() > 0 ? "buy" : "sell",
                    Pair = pair
                };

                return trade;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse trade data", ex);
            }
        }
    }
}
