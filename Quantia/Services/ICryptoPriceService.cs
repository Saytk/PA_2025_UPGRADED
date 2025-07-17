// Services/ICryptoPriceService.cs
using System.Net.Http.Json;
public interface ICryptoPriceService
{
    Task<decimal?> GetLastPriceAsync(string symbol);  // null si feed indispo
}

// Services/CryptoPriceService.cs  (exemple: via votre API FastAPI)

public class CryptoPriceService : ICryptoPriceService
{
    private readonly HttpClient _http;
    public CryptoPriceService(IHttpClientFactory factory) => _http = factory.CreateClient();

    public async Task<decimal?> GetLastPriceAsync(string symbol)
    {
        try
        {
            var url = $"http://127.0.0.1:8000/data/{symbol}?days=1&interval=1m";
            var json = await _http.GetFromJsonAsync<dynamic>(url);
            return (decimal?)json?.last_close;
        }
        catch { return null; }
    }
}
