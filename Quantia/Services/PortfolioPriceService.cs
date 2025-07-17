using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Quantia.Services
{
    /// <summary>Service prix : lit /data/{symbol} (format objet OU tableau).</summary>
    public class PortfolioPriceService
    {
        private readonly HttpClient _http;
        private const string API = "https://api-test-049u.onrender.com";

        public PortfolioPriceService(IHttpClientFactory factory)
            => _http = factory.CreateClient();

        /*──────────── Dernier prix (optimisé) ────────────*/
        public async Task<decimal?> GetLatestPrice(string symbol)
        {
            try
            {
                var url = $"{API}/data/{symbol}/last_candle";
                var json = await _http.GetFromJsonAsync<LastCandleResponse>(url);
                return json?.PriceUsdt;
            }
            catch
            {
                return null;
            }
        }

        /*──── Prix minute le plus proche d’une date ────*/
        public async Task<decimal?> GetHistoricalPrice(string symbol, DateTime utcTime)
        {
            var days = Math.Max(1, (DateTime.UtcNow.Date - utcTime.Date).Days + 1);
            var url = $"{API}/data/{symbol}?days={days}&interval=1m&raw=true";

            var candles = await FetchCandles(url);
            if (candles == null || candles.Count == 0) return null;

            var candle = candles
                .Where(c => DateTime.TryParse(c.TimestampUtc, out var t) && t <= utcTime)
                .OrderByDescending(c => DateTime.Parse(c.TimestampUtc))
                .FirstOrDefault();

            return candle?.Close ?? candle?.Price;
        }

        /*──────── Fetch + désérialisation robuste ───────*/
        private async Task<List<Candle>?> FetchCandles(string url)
        {
            try
            {
                var json = await _http.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(json)) return null;

                json = json.TrimStart();
                if (json.StartsWith("{"))
                {
                    var obj = JsonSerializer.Deserialize<DataResponse>(json);
                    return obj?.Data;
                }
                if (json.StartsWith("["))
                {
                    return JsonSerializer.Deserialize<List<Candle>>(json);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /*──────── DTO internes ───────*/

        // Pour /data/{symbol}/last_candle
        private class LastCandleResponse
        {
            [JsonPropertyName("price_usdt")] public decimal PriceUsdt { get; set; }
        }

        // Pour /data/{symbol}?raw=true
        private class DataResponse
        {
            [JsonPropertyName("data")]
            public List<Candle> Data { get; set; } = new();
        }

        private class Candle
        {
            [JsonPropertyName("timestamp_utc")] public string TimestampUtc { get; set; } = "";
            [JsonPropertyName("close")] public decimal? Close { get; set; }
            [JsonPropertyName("price")] public decimal? Price { get; set; }
        }
    }
}
