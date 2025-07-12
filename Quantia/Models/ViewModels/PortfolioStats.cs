namespace Quantia.Models.ViewModels
{
    /// <summary>Statistiques agr�g�es du portefeuille</summary>
    public class PortfolioStats
    {
        public decimal Balance { get; set; }
        public decimal UnrealizedPnL { get; set; }
        public decimal WinRate { get; set; }
        public decimal ProfitFactor { get; set; }
    }
}
