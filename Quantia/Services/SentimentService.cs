using Quantia.Data;

namespace Quantia.Services;

public sealed class SentimentService
{
    private readonly ISentimentRepository _repo;
    private readonly ILogger<SentimentService> _log;

    public SentimentService(ISentimentRepository repo,
                            ILogger<SentimentService> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<SentimentDto> GetLatestAsync(CancellationToken ct = default)
    {
        var dto = await _repo.GetLatestAsync(ct)
                  ?? throw new InvalidOperationException("Aucune donnée sentiment en base.");
        _log.LogInformation("Sentiment récupéré : {Index}", dto.Global_Index);
        return dto;
    }
}
