// Controllers/PredictionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private const string ML_API_BASE = "http://127.0.0.1:8000";

        private readonly IHttpClientFactory _http;
        private readonly PortfolioEquityService _equityService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly AppDbContext _db;          // ← NEW

        public PredictionController(
            IHttpClientFactory httpFactory,
            PortfolioEquityService equityService,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext dbContext)                // ← NEW
        {
            _http = httpFactory;
            _equityService = equityService;
            _httpContext = httpContextAccessor;
            _db = dbContext;                       // ← NEW
        }

        private HttpClient MlClient => _http.CreateClient();

        /*---------------------------------------------------------------*
         *  PAGE PRINCIPALE
         *---------------------------------------------------------------*/
        [HttpGet("")]
        public async Task<IActionResult> Index(string symbol = "BTCUSDT")
            => View("Index", await BuildViewModel(symbol));

        /*---------------------------------------------------------------*
         *  ENDPOINT JSON (Polling)
         *---------------------------------------------------------------*/
        [HttpGet("json")]
        public async Task<IActionResult> GetJson(string symbol = "BTCUSDT")
            => Json(await BuildViewModel(symbol));

        /*---------------------------------------------------------------*
         *  CONSTRUCTION DU VIEWMODEL
         *---------------------------------------------------------------*/
        private async Task<TradePredictionVM> BuildViewModel(string symbol)
        {
            /*=== 1. Appel modèle ML ======================================*/
            var raw = await MlClient.GetFromJsonAsync<PredictionResponse>(
                          $"{ML_API_BASE}/prediction/latest?symbol={symbol}");

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

            /*=== 2. Trades déjà enregistrés ==============================*/
            int userId = int.Parse(_httpContext.HttpContext!
                                   .User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var trades = await _db.Trades                          // DbSet<TradeModel>
                                  .Where(t => t.UserId == userId
                                           && t.CryptoSymbol == symbol)
                                  .OrderBy(t => t.BuyDate)
                                  .ToListAsync();

            /*=== 3. Equity & stats ======================================*/
            var (dates, values) = await _equityService.GetEquityAsync(userId, 30);
            var stats = await _equityService.GetStatsAsync(userId);

            /*=== 4. VM final ============================================*/
            return new TradePredictionVM
            {
                EquityDates = dates,
                EquityValues = values,
                Balance = stats.Balance,
                UnrealizedPnl = stats.UnrealizedPnL,
                WinRate = stats.WinRate,
                ProfitFactor = stats.ProfitFactor,

                Signals = signals,
                ExecutedTrades = trades        // ← NEW  (ajoute la liste à la vue)
            };
        }

        /*---------------------------------------------------------------*
         *  ACTIONS EXISTANTES
         *---------------------------------------------------------------*/
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

        /*---------------------------------------------------------------*/
        public record PipelineRequest(string Mode, string Symbol, int Days, string? ModelPath);
        public record PredictRequest(string ApiUrl, string Symbol);


        // POST /Prediction/GetPredictions
        [HttpPost("GetPredictions")]
        public async Task<IActionResult> GetPredictions([FromBody] PredictRequest dto)
        {
            try
            {
                // exemple : http://localhost:8000/pattern/predict-lates
                var url = $"{dto.ApiUrl}?symbol={dto.Symbol}";
                var json = await MlClient.GetStringAsync(url);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Problem($"Error fetching predictions: {ex.Message}");
            }
        }
    }
}
