using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Quantia.Models;
using Quantia.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Quantia.Controllers
{
    [Route("Prediction")]
    public class PredictionController : Controller
    {
        private const string ML_API_BASE = "http://localhost:8009";
        private readonly IHttpClientFactory _http;

        public PredictionController(IHttpClientFactory httpFactory) => _http = httpFactory;

        private HttpClient MlClient => _http.CreateClient();         // vers FastAPI
        private HttpClient LocalClient => _http.CreateClient();        // interne ASP.NET

        /* --------------------- DASHBOARD -------------------------------- */
        [HttpGet("")]
        public async Task<IActionResult> Index(string symbol = "BTCUSDT")
        {
            // 1. Signaux ML
            var signals = await MlClient.GetFromJsonAsync<List<TradeSignal>>
                ($"{ML_API_BASE}/signals?symbol={symbol}&limit=10") ?? new();

            // 2. Courbe d’équity réelle (portefeuille)
            var eqJson = await LocalClient.GetStringAsync("/Portfolio/Equity?days=30");
            var curve = JsonConvert.DeserializeObject<Dictionary<string, List<object>>>(eqJson)!;
            var dates = curve["dates"].ConvertAll(d => DateTime.Parse(d.ToString()!));
            var values = curve["equity"].ConvertAll(v => Convert.ToDecimal(v!));

            // 3. Stats portefeuille
            var stats = await LocalClient.GetFromJsonAsync<PortfolioStats>("/Portfolio/Stats")
                        ?? new PortfolioStats();

            var vm = new TradePredictionVM
            {
                EquityDates = dates,
                EquityValues = values,
                Signals = signals,
                Balance = stats.Balance,
                UnrealizedPnl = stats.UnrealizedPnL,
                WinRate = stats.WinRate,
                ProfitFactor = stats.ProfitFactor
            };
            return View("TradePrediction", vm);  // Razor dans Views/Prediction/
        }

        /* --------------------- UTILITAIRES ML --------------------------- */

        [HttpPost("RefreshModel")]
        public async Task<IActionResult> RefreshModel()
        {
            var resp = await MlClient.PostAsync($"{ML_API_BASE}/refresh-model", null);
            return Content(await resp.Content.ReadAsStringAsync(), "application/json");
        }

        [HttpPost("RunMlPipeline")]
        public async Task<IActionResult> RunMlPipeline([FromBody] PipelineRequest dto)
        {
            var resp = await MlClient.PostAsJsonAsync($"{ML_API_BASE}/run_ml_pipeline", dto);
            return Content(await resp.Content.ReadAsStringAsync(), "application/json");
        }

        [HttpGet("GetModelMetrics")]
        public async Task<IActionResult> GetMetrics(string? model)
        {
            var url = $"{ML_API_BASE}/get_model_metrics";
            if (!string.IsNullOrWhiteSpace(model)) url += $"?model={model}";
            var json = await MlClient.GetStringAsync(url);
            return Content(json, "application/json");
        }

        public record PipelineRequest(string Mode, string Symbol, int Days, string? ModelPath);
    }
}
