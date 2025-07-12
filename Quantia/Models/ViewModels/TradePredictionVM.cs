using System;
using System.Collections.Generic;
using Quantia.Models;

namespace Quantia.Models.ViewModels
{
    /// <summary>ViewModel combinant equity, signaux et stats</summary>
    public class TradePredictionVM
    {
        /* ─────────── Courbe d’équité ─────────── */
        public List<DateTime> EquityDates { get; set; } = new();
        public List<decimal> EquityValues { get; set; } = new();

        /* ─────────── Signaux actifs ──────────── */
        public List<TradeSignal> Signals { get; set; } = new();

        /* ─────────── Stats individuelles ─────── */
        public decimal Balance { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public decimal WinRate { get; set; }
        public decimal ProfitFactor { get; set; }

        /* ─────────── Stats groupées (facultatif) ──────
           Certaines vues utilisent Model.Stats …  */
        public PortfolioStats Stats
        {
            get => new PortfolioStats
            {
                Balance = Balance,
                UnrealizedPnL = UnrealizedPnl,
                WinRate = WinRate,
                ProfitFactor = ProfitFactor
            };
            set
            {
                if (value == null) return;
                Balance = value.Balance;
                UnrealizedPnl = value.UnrealizedPnL;
                WinRate = value.WinRate;
                ProfitFactor = value.ProfitFactor;
            }
        }
    }
}
