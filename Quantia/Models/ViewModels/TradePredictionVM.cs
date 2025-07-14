using System;
using System.Collections.Generic;

namespace Quantia.Models.ViewModels
{
    /// <summary>
    /// Vue combinant equity, signaux, stats, suggestion détaillée
    /// et historique des trades exécutés.
    /// </summary>
    public class TradePredictionVM
    {
        /* ─────── Courbe d’équité ─────── */
        public List<DateTime> EquityDates { get; set; } = new();
        public List<decimal> EquityValues { get; set; } = new();

        /* ─────── Signaux actifs ─────── */
        public List<TradeSignal> Signals { get; set; } = new();

        /* ─────── Dernière suggestion ─────── */
        public TradeSuggestion? Suggestion { get; set; }

        /* ─────── Stats individuelles ─────── */
        public decimal Balance { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public decimal WinRate { get; set; }
        public decimal ProfitFactor { get; set; }

        /* ─────── Trades déjà enregistrés ─────── */
        public IEnumerable<TradeModel> ExecutedTrades { get; set; }
            = Array.Empty<TradeModel>();

        /* ─────── Agrégation facultative ─────── */
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
