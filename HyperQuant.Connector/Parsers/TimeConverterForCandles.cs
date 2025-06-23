namespace HyperQuant.Connector.Parsers
{
    public static class TimeConverterForCandles
    {
        private static readonly Dictionary<int, string> TimeMap = new()
        {
            [60] = "1m",
            [300] = "5m",
            [900] = "15m",
            [1800] = "30m",
            [3600] = "1h",
            [10800] = "3h",
            [21600] = "6h",
            [43200] = "12h",
            [86400] = "1D",
            [604800] = "7D",
            [2592000] = "1M"
        };

        public static string ConvertSecondsToBitfinexTime(int seconds)
        {
            if (TimeMap.TryGetValue(seconds, out var time))
            {
                return time;
            }

            throw new ArgumentException($"Unsupported time: {seconds} seconds");
        }
    }
}
