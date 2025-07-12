using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Quantia.Services
{
    public class PortfolioPriceService
    {
        private readonly HttpClient _http;
        public PortfolioPriceService(HttpClient http) => _http = http;

        /* ───────────────────────── Dernier prix ───────────────────────── */
        public async Task<decimal?> GetLatestPrice(string symbol)
        {
            var url = $"http://localhost:8000/data/{symbol}?days=1&interval=1m";

            var resp = await _http.GetFromJsonAsync<DataResponse>(url);
            if (resp?.Data == null || resp.Data.Count == 0) return null;

            var last = resp.Data.Last();
            return last.Close ?? last.Price;
        }

        /* ─────────────── Prix minute le plus proche d’une date ─────────── */
        public async Task<decimal?> GetHistoricalPrice(string symbol, DateTime utcTime)
        {
            // Nombre de jours à remonter depuis maintenant (minimum 1)
            var daysBack = Math.Max(1, (DateTime.UtcNow.Date - utcTime.Date).Days + 1);
            var url = $"http://localhost:8000/data/{symbol}?days={daysBack}&interval=1m";

            var resp = await _http.GetFromJsonAsync<DataResponse>(url);
            if (resp?.Data == null || resp.Data.Count == 0) return null;

            var candle = resp.Data
                             .Where(c => DateTime.Parse(c.TimestampUtc) <= utcTime)
                             .OrderByDescending(c => DateTime.Parse(c.TimestampUtc))
                             .FirstOrDefault();

            return candle?.Close ?? candle?.Price;
        }

        /* ──────────────────────── DTO de désérialisation ──────────────── */
        private class DataResponse
        {
            [JsonPropertyName("data")]
            public List<Candle> Data { get; set; } = new();
        }

        private class Candle
        {
            [JsonPropertyName("timestamp_utc")]
            public string TimestampUtc { get; set; } = "";

            // Le endpoint /data renvoie au moins "close"; on garde aussi "price"
            [JsonPropertyName("close")]
            public decimal? Close { get; set; }

            [JsonPropertyName("price")]
            public decimal? Price { get; set; }
        }
    }
}
