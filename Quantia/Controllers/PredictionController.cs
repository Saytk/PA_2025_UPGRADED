using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace Quantia.Controllers
{
    [Route("Prediction")]
    public class PredictionController : Controller
    {
        /* ------------------------------------------------------------------ */
        /*  CONFIGURATION                                                     */
        /* ------------------------------------------------------------------ */
        private const string PYTHON_API_BASE = "http://localhost:8009"; // <- en dur
        private readonly IHttpClientFactory _httpFactory;

        public PredictionController(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

        private HttpClient Client => _httpFactory.CreateClient();

        /* ------------------------------------------------------------------ */
        /*  VUES                                                              */
        /* ------------------------------------------------------------------ */

        // GET /Prediction
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // GET /Prediction/Pipeline
        [HttpGet("Pipeline")]
        public IActionResult Pipeline() => View();           // Views/Prediction/Pipeline.cshtml


        /* ------------------------------------------------------------------ */
        /*  ENDPOINTS JSON – appelés par JavaScript                           */
        /* ------------------------------------------------------------------ */

        // POST /Prediction/GetPredictions
        [HttpPost("GetPredictions")]
        public async Task<IActionResult> GetPredictions([FromBody] PredictRequest dto)
        {
            try
            {
                // exemple : http://localhost:8009/predict-latest?symbol=BTCUSDT
                var url = $"{dto.ApiUrl}?symbol={dto.Symbol}";
                var json = await Client.GetStringAsync(url);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Problem($"Error fetching predictions: {ex.Message}");
            }
        }

        // POST /Prediction/RefreshModel
        [HttpPost("RefreshModel")]
        public async Task<IActionResult> RefreshModel([FromBody] RefreshRequest dto)
        {
            try
            {
                var resp = await Client.PostAsync($"{PYTHON_API_BASE}/refresh-model", null);
                var json = await resp.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Problem($"Error refreshing model: {ex.Message}");
            }
        }

        // POST /Prediction/RunMlPipeline
        [HttpPost("RunMlPipeline")]
        public async Task<IActionResult> RunMlPipeline([FromBody] PipelineRequest dto)
        {
            try
            {
                var resp = await Client.PostAsJsonAsync($"{PYTHON_API_BASE}/run_ml_pipeline", dto);
                var json = await resp.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Problem($"Pipeline error: {ex.Message}");
            }
        }

        // GET /Prediction/GetModelMetrics?model=xgb_direction
        [HttpGet("GetModelMetrics")]
        public async Task<IActionResult> GetModelMetrics([FromQuery] string? model)
        {
            try
            {
                var url = $"{PYTHON_API_BASE}/get_model_metrics";
                if (!string.IsNullOrWhiteSpace(model))
                    url += $"?model={Uri.EscapeDataString(model)}";

                var json = await Client.GetStringAsync(url);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Problem($"Error metrics: {ex.Message}");
            }
        }

        /* ------------------------------------------------------------------ */
        /*  DTOs                                                              */
        /* ------------------------------------------------------------------ */
        public record PredictRequest(string ApiUrl, string Symbol);
        public record RefreshRequest(string ApiUrl);
        public record PipelineRequest(string Mode, string Symbol, int Days, string? ModelPath);
    }
}
