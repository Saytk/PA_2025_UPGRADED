using System;

namespace Quantia.Models
{
    /// <summary>Signal transform� pour l�affichage UI</summary>
    public class TradeSignal
    {
        public DateTime Timestamp { get; set; }
        public string Symbol { get; set; } = "BTCUSDT";
        public string Side { get; set; } = "BUY";     // BUY / SELL
        public decimal Probability { get; set; }
        public decimal Entry { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal PositionSize { get; set; }              // Ajout�
        public decimal Confidence { get; set; }              // Ajout�
        public string? Note { get; set; }              // Ajout�
        public string? Strategy { get; set; }              // Ajout�

        public decimal RiskReward =>
            StopLoss == 0 ? 0 :
            Math.Round(Math.Abs(TakeProfit - Entry) /
                       Math.Abs(Entry - StopLoss), 2);
    }
}
