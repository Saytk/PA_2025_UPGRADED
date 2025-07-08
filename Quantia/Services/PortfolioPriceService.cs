using System.Net.Http.Json;
using Quantia.Models;

namespace Quantia.Services
{
    public class PortfolioPriceService
    {
        private readonly HttpClient _http;
        public PortfolioPriceService(HttpClient http) => _http = http;

        public async Task<decimal?> GetLatestPrice(string symbol)
        {
            var url =
                $"http://127.0.0.1:8003/load-data?symbol={symbol}&" +
                $"start_date={DateTime.UtcNow:yyyy-MM-dd}&" +
                $"end_date={DateTime.UtcNow:yyyy-MM-dd}";

            var apiResp = await _http.GetFromJsonAsync<PriceApiResponse>(url);
            var last = apiResp?.Data.LastOrDefault();
            return last?.Price;   // null si pas de prix
        }
    }

}