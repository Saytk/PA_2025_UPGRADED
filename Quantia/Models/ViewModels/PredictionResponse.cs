namespace Quantia.Models
{
    public class PredictionResponse
    {
        public string symbol { get; set; } = "";
        public DateTime timestamp { get; set; }
        public double prob_up { get; set; }
        public string signal { get; set; } = "";
        public double confidence { get; set; }
        public decimal entry { get; set; }
        public decimal stop_loss { get; set; }
        public decimal take_profit { get; set; }
        public string? note { get; set; }
    }
}
