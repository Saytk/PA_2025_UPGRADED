using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Quantia.Models; // adapte selon ton namespace

public class DashboardController : Controller
{
    private readonly HttpClient _httpClient;

    public DashboardController()
    {
        _httpClient = new HttpClient();
    }

    public async Task<ActionResult> Index()
    {
        string topic = "gold";
        int limit = 10;

        string apiUrl = $"http://192.168.0.26:8000/analyze?topic={topic}&limit={limit}";

        RedditAnalysisResultModel result = null;

        try
        {
            result = await _httpClient.GetFromJsonAsync<RedditAnalysisResultModel>(apiUrl);
        }
        catch (Exception ex)
        {
            // Tu peux logguer ici
            ViewBag.ApiError = "Erreur lors de l'appel API : " + ex.Message;
        }

        return View(result);
    }
}