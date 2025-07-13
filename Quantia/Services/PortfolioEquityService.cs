using Quantia.Models;
using Quantia.Services;
using Microsoft.EntityFrameworkCore;
using Quantia.Models.ViewModels;

namespace Quantia.Services
{
    public class PortfolioEquityService
    {
        private readonly AppDbContext _context;
        private readonly PortfolioPriceService _price;

        public PortfolioEquityService(AppDbContext context, PortfolioPriceService priceService)
        {
            _context = context;
            _price = priceService;
        }

        public async Task<(List<DateTime> Dates, List<decimal> Values)> GetEquityAsync(int userId, int days = 30)
        {
            var since = DateTime.UtcNow.AddDays(-days);
            var rows = await _context.Transactions
                .Where(t => t.UserId == userId && t.Timestamp >= since)
                .OrderBy(t => t.Timestamp)
                .ToListAsync();

            var dates = new List<DateTime>();
            var equity = new List<decimal>();
            decimal balance = 0;

            foreach (var tx in rows)
            {
                var price = await _price.GetHistoricalPrice(tx.CryptoSymbol, tx.Timestamp)
                            ?? tx.PriceAtPurchase;

                balance += tx.Amount * price;
                dates.Add(tx.Timestamp);
                equity.Add(balance);
            }

            return (dates, equity);
        }

        public async Task<PortfolioStats> GetStatsAsync(int userId)
        {
            var rows = await _context.Transactions
                                     .Where(t => t.UserId == userId)
                                     .ToListAsync();

            if (!rows.Any()) return new PortfolioStats();

            decimal balance = 0, invested = 0, wins = 0, losses = 0;

            foreach (var grp in rows.GroupBy(r => r.CryptoSymbol))
            {
                var last = await _price.GetLatestPrice(grp.Key);
                if (last is null) continue;

                var qty = grp.Sum(r => r.Amount);
                var curVal = qty * last.Value;
                var invVal = grp.Sum(r => r.Amount * r.PriceAtPurchase);

                balance += curVal;
                invested += invVal;

                var pnl = curVal - invVal;
                if (pnl >= 0) wins += pnl; else losses += -pnl;
            }

            return new PortfolioStats
            {
                Balance = balance,
                UnrealizedPnL = balance - invested,
                WinRate = wins + losses == 0 ? 0 : wins / (wins + losses),
                ProfitFactor = losses == 0 ? wins : wins / losses
            };
        }
    }
}
