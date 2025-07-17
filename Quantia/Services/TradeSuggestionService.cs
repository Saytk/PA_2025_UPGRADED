using Quantia.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Quantia.Services
{
    public class TradeSuggestionService
    {
        private readonly HttpClient _http;
        private const string BASE = "http://127.0.0.1:8000";

        public TradeSuggestionService(HttpClient http) => _http = http;

        /// <summary>Appelle /trade/suggest ; retourne null si l’API renvoie un statut 4xx/5xx</summary>
        public async Task<TradeSuggestion?> GetSuggestionAsync(
            string symbol,
            decimal riskMultiple,
            string? jwt = null)
        {
            var uri = $"{BASE}/trade/suggest?symbol={symbol}&risk_multiple={riskMultiple}";

            if (!string.IsNullOrWhiteSpace(jwt))
                _http.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", jwt);

            try
            {
                return await _http.GetFromJsonAsync<TradeSuggestion>(uri);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
