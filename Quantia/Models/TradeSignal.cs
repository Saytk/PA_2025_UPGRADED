using System;

namespace Quantia.Models
{
    /// <summary>Signal unique généré par l’API ML</summary>
    public class TradeSignal
    {
        public DateTime Timestamp { get; set; }
        public string Symbol { get; set; } = "BTCUSDT";
        public decimal Probability { get; set; }
        public decimal Entry { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public string Side { get; set; } = "BUY";   // BUY / SELL

        public decimal RiskReward =>
            StopLoss == 0 ? 0 :
            Math.Round(Math.Abs(TakeProfit - Entry) / Math.Abs(Entry - StopLoss), 2);
    }
}
