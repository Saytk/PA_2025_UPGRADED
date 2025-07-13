using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Quantia.Models;
using Quantia.Models.ViewModels;
using Quantia.Services;
using System;
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

        [HttpGet("")]
        public async Task<IActionResult> Index(string symbol = "BTCUSDT")
        {
            var rawPrediction = await MlClient.GetFromJsonAsync<PredictionSignal>(
                $"{ML_API_BASE}/prediction/latest?symbol={symbol}") ?? new();

            var signals = new List<TradeSignal>
            {
                new TradeSignal
                {
                    Timestamp = rawPrediction.timestamp,
                    Symbol = rawPrediction.symbol,
                    Probability = (Decimal)rawPrediction.prob_up,
                    Side = rawPrediction.signal,
                    Entry = 0,
                    StopLoss = 0,
                    TakeProfit = 0
                }
            };

            int userId = int.Parse(_httpContext.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var (dates, values) = await _equityService.GetEquityAsync(userId, 30);
            var stats = await _equityService.GetStatsAsync(userId);

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

            return View("TradePrediction", vm);
        }

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
