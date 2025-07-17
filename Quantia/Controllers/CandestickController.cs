using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;                 // pour UrlEncode

namespace Quantia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandlestickController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _backendBaseUrl;  

        public CandlestickController(IHttpClientFactory httpClientFactory,
                                     IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _backendBaseUrl = config["BackendBaseUrl"]?.TrimEnd('/')
                                ?? "http://127.0.0.1:8000";
        }

        /* ------------------------------------------------------------
         *  Méthode utilitaire : proxy GET → JSON
         * ----------------------------------------------------------*/
        private async Task<IActionResult> ProxyAsync(string relativeUrl)
        {
            var resp = await _httpClient.GetAsync($"{_backendBaseUrl}{relativeUrl}");

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode,
                                  "Failed to retrieve data from backend API.");

            var json = await resp.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        /* ------------------------------------------------------------
         *  1) Bougies simples
         * ----------------------------------------------------------*/
        [HttpGet("load")]
        public Task<IActionResult> LoadCandles(
            [FromQuery] string symbol,
            [FromQuery] string start_date,
            [FromQuery] string end_date)
        {
            var q = $"?symbol={HttpUtility.UrlEncode(symbol)}" +
                    $"&start_date={HttpUtility.UrlEncode(start_date)}" +
                    $"&end_date={HttpUtility.UrlEncode(end_date)}";

            return ProxyAsync($"/pattern/load-data{q}");
        }

        /* ------------------------------------------------------------
         *  2) Dernière prédiction ML / statistique
         * ----------------------------------------------------------*/
        [HttpGet("predict")]
        public Task<IActionResult> GetPredictions([FromQuery] string symbol)
        {
            var q = $"?symbol={HttpUtility.UrlEncode(symbol)}";
            return ProxyAsync($"/prediction/latest{q}");
        }

        /* ------------------------------------------------------------
         *  3) Patterns « data-driven » (ton endpoint existant)
         * ----------------------------------------------------------*/
        [HttpGet("patterns")]
        public Task<IActionResult> LoadPatterns(
            [FromQuery] string symbol,
            [FromQuery] string start_date,
            [FromQuery] string end_date)
        {
            var q = $"?symbol={HttpUtility.UrlEncode(symbol)}" +
                    $"&start_date={HttpUtility.UrlEncode(start_date)}" +
                    $"&end_date={HttpUtility.UrlEncode(end_date)}";

            return ProxyAsync($"/pattern/load-data-patterns{q}");
        }

        /* ------------------------------------------------------------
         *  4) NEW – Patterns chandeliers « classiques »
         * ----------------------------------------------------------*/
        [HttpGet("patterns/classic")]
        public Task<IActionResult> LoadClassicPatterns(
            [FromQuery] string symbol,
            [FromQuery] string start_date,
            [FromQuery] string end_date,
            [FromQuery] double atr_min_pct = 0.05)   // filtre volatilité
        {
            var q = $"?symbol={HttpUtility.UrlEncode(symbol)}" +
                    $"&start_date={HttpUtility.UrlEncode(start_date)}" +
                    $"&end_date={HttpUtility.UrlEncode(end_date)}" +
                    $"&atr_min_pct={atr_min_pct.ToString(CultureInfo.InvariantCulture)}";

            // route définie côté FastAPI :  /api/candlestick/patterns/classic
            return ProxyAsync($"/pattern/load-data-patterns-classic{q}");
        }
    }
}
