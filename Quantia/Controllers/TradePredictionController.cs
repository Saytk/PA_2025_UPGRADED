using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Quantia.Models;
using Quantia.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Quantia.Controllers
{
    public class TradePredictionController : Controller
    {
        private readonly HttpClient _api = new()
        {
            BaseAddress = new Uri("http://localhost:8000")   // API Python
        };

        public async Task<IActionResult> Index(string symbol = "BTCUSDT")
        {
            // 1 – Signaux
            var sigResp = await _api.GetAsync($"/signals?symbol={symbol}&limit=10");
            if (!sigResp.IsSuccessStatusCode) return View("Error");
            var signals = JsonConvert.DeserializeObject<List<TradeSignal>>
                          (await sigResp.Content.ReadAsStringAsync());

            // 2 – Équité
            var eqResp = await _api.GetAsync($"/equity?symbol={symbol}&days=30");
            if (!eqResp.IsSuccessStatusCode) return View("Error");
            var eqJson = JsonConvert.DeserializeObject<Dictionary<string, List<object>>>
                         (await eqResp.Content.ReadAsStringAsync());
            var dates = eqJson["dates"].ConvertAll(d => DateTime.Parse(d.ToString()));
            var values = eqJson["equity"].ConvertAll(v => Convert.ToDecimal(v));

            // 3 – Stats portefeuille
            var stResp = await _api.GetAsync("/portfolio/stats");
            if (!stResp.IsSuccessStatusCode) return View("Error");
            var stats = JsonConvert.DeserializeObject<PortfolioStats>
                        (await stResp.Content.ReadAsStringAsync());

            // 4 – VM
            var vm = new TradePredictionVM
            {
                EquityDates = dates,
                EquityValues = values,
                Signals = signals,
                Stats = stats
            };
            return View(vm);
        }
    }
}
