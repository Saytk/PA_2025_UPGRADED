// Models/PredictionSignal.cs
namespace Quantia.Models
{
    public class PredictionSignal
    {
        public string symbol { get; set; } = string.Empty;
        public DateTime timestamp { get; set; }
        public float prob_up { get; set; }
        public string signal { get; set; } = string.Empty;
        public decimal confidence { get; set; }
        public bool UsingIncompleteCandle { get; set; }
    }
}
