namespace Quantia.Services;

public sealed class SentimentDto
{
    public double Global_Index { get; init; }
    public List<ClusterDto> Clusters { get; init; } = [];
}

public sealed class ClusterDto
{
    public string Topic { get; init; } = "";
    public double Avg { get; init; }
    public int Freq { get; init; }
    public double Delta { get; init; }
    public string Summary { get; init; } = "";
    public List<string> FullMessages { get; init; } = [];
    public List<string> Examples { get; init; } = [];

    public List<string> Urls { get; init; } = [];
}
