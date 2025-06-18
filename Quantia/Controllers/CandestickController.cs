using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace Quantia.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandlestickController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public CandlestickController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet("load")]
        public async Task<IActionResult> LoadCandles([FromQuery] string symbol, [FromQuery] string start_date, [FromQuery] string end_date)
        {
            var apiUrl = $"http://127.0.0.1:8003/load-data?symbol={symbol}&start_date={start_date}&end_date={end_date}";
            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to retrieve data from the backend API.");
            }

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        [HttpGet("predict")]
        public async Task<IActionResult> GetPredictions([FromQuery] string symbol)
        {
            var url = $"http://127.0.0.1:8003/predict-latest?symbol={symbol}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to retrieve prediction data from backend API.");
            }

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

    }


}
