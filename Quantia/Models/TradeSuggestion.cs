using System;

namespace Quantia.Models
{
    /// <summary>Réponse brute de l’endpoint /trade/suggest</summary>
    public class TradeSuggestion
    {
        public string Symbol { get; set; } = "";
        public string Side { get; set; } = "";            // LONG / SHORT
        public decimal EntryPrice { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal PositionSize { get; set; }                  // En unités crypto
        public decimal Confidence { get; set; }                  // 0–1
        public DateTime Timestamp { get; set; }
    }
}
