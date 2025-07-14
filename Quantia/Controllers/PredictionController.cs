using Microsoft.AspNetCore.Mvc;
using Quantia.Models;
using Quantia.Models.ViewModels;
using Quantia.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Quantia.Controllers
{
    [Route("Prediction")]
    public class PredictionController : Controller
    {
        private const string ML_API_BASE = "http://localhost:8000";

        private readonly IHttpClientFactory _http;
        private readonly PortfolioEquityService _equityService;
        private readonly IHttpContextAccessor _httpContext;

        public PredictionController(
            IHttpClientFactory httpFactory,
            PortfolioEquityService equityService,
            IHttpContextAccessor httpContextAccessor)
        {
            _http = httpFactory;
            _equityService = equityService;
            _httpContext = httpContextAccessor;
        }

        private HttpClient MlClient => _http.CreateClient();

        /* ------------------------------------------------------------------ */
        /*  PAGE PRINCIPALE                                                   */
        /* ------------------------------------------------------------------ */
        [HttpGet("")]
        public async Task<IActionResult> Index(string symbol = "BTCUSDT")
        {
            var vm = await BuildViewModel(symbol);
            return View("TradePrediction", vm);
        }

        /* ------------------------------------------------------------------ */
        /*  ENDPOINT JSON (Polling)                                           */
        /* ------------------------------------------------------------------ */
        [HttpGet("json")]
        public async Task<IActionResult> GetJson(string symbol = "BTCUSDT")
            => Json(await BuildViewModel(symbol));

        /* ------------------------------------------------------------------ */
        /*  CONSTRUCTION DU VIEWMODEL                                         */
        /* ------------------------------------------------------------------ */
        private async Task<TradePredictionVM> BuildViewModel(string symbol)
        {
            /* ---- 1. Appel à l’API ML (fortement typé) -------------------- */
            var raw = await MlClient.GetFromJsonAsync<PredictionResponse>(
                          $"{ML_API_BASE}/prediction/latest?symbol={symbol}");

            /* ---- 2. Mapping vers TradeSignal ----------------------------- */
            var signals = new List<TradeSignal>();
            if (raw is not null)
            {
                signals.Add(new TradeSignal
                {
                    Timestamp = raw.timestamp,
                    Symbol = raw.symbol,
                    Probability = (decimal)raw.prob_up,
                    Side = raw.signal == "LONG" ? "BUY" : "SELL",
                    Entry = raw.entry,
                    StopLoss = raw.stop_loss,
                    TakeProfit = raw.take_profit,
                    Confidence = (decimal)raw.confidence,
                    PositionSize = (decimal)raw.confidence * 100,
                    Note = raw.note,
                    Strategy = "auto_sl_tp"
                });
            }

            /* ---- 3. Équity & stats utilisateur --------------------------- */
            int userId = int.Parse(_httpContext.HttpContext!
                                   .User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (dates, values) = await _equityService.GetEquityAsync(userId, 30);
            var stats = await _equityService.GetStatsAsync(userId);

            /* ---- 4. ViewModel final -------------------------------------- */
            return new TradePredictionVM
            {
                EquityDates = dates,
                EquityValues = values,
                Signals = signals,
                Balance = stats.Balance,
                UnrealizedPnl = stats.UnrealizedPnL,
                WinRate = stats.WinRate,
                ProfitFactor = stats.ProfitFactor
            };
        }

        /* ------------------------------------------------------------------ */
        /*  ACTIONS EXISTANTES                                                */
        /* ------------------------------------------------------------------ */
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

        /* ------------------------------------------------------------------ */
        public record PipelineRequest(string Mode, string Symbol, int Days, string? ModelPath);
    }
}
