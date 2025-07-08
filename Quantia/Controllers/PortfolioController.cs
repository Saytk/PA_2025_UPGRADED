using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Models;
using Quantia.Models.ViewModels;
using Quantia.Services;
using System.Security.Claims;

namespace Quantia.Controllers
{
    [Authorize]
    public class PortfolioController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PortfolioPriceService _priceService;

        public PortfolioController(AppDbContext context, PortfolioPriceService priceService)
        {
            _context = context;
            _priceService = priceService;
        }

        // ─────────────────────────────
        // GET  /Portfolio               → tableau PnL
        // ─────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var transactions = await _context.Transactions
                                             .Where(t => t.UserId == userId)
                                             .ToListAsync();

            var grouped = transactions
                .GroupBy(t => t.CryptoSymbol)
                .Select(g => new
                {
                    Symbol = g.Key,
                    Quantity = g.Sum(t => t.Amount),
                    Invested = g.Sum(t => t.Amount * t.PriceAtPurchase)
                });

            var portfolio = new List<PortfolioRow>();

            foreach (var g in grouped)
            {
                var latestPrice = await _priceService.GetLatestPrice(g.Symbol);
                if (latestPrice == null) continue;

                var currentValue = g.Quantity * latestPrice.Value;

                portfolio.Add(new PortfolioRow
                {
                    Symbol = g.Symbol,
                    Quantity = g.Quantity,
                    Invested = g.Invested,
                    CurrentPrice = latestPrice.Value,
                    CurrentValue = currentValue,
                    PnL = currentValue - g.Invested
                });
            }

            return View(portfolio);
        }

        // ─────────────────────────────
        // GET  /Portfolio/Create        → formulaire « Simulate Buy »
        // ─────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View(new NewTransactionModel());
        }

        // ─────────────────────────────
        // POST /Portfolio/Create        → enregistre la transaction
        // ─────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewTransactionModel m)
        {
            if (!ModelState.IsValid) return View(m);

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (m.PriceAtPurchase is null or 0)
            {
                var price = await _priceService.GetLatestPrice(m.CryptoSymbol.ToUpper());
                if (price is null)
                {
                    ModelState.AddModelError("", $"Price for {m.CryptoSymbol} not found.");
                    return View(m);
                }
                m.PriceAtPurchase = price.Value;
            }


            _context.Transactions.Add(new Transaction
            {
                UserId = userId,
                CryptoSymbol = m.CryptoSymbol.ToUpper(),
                Amount = m.Amount,
                PriceAtPurchase = (decimal)m.PriceAtPurchase,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Add(string cryptoSymbol, decimal amount, decimal priceAtPurchase)
        {
            int userId = 5/* récupère l’ID utilisateur */;

            _context.Transactions.Add(new Transaction
            {
                UserId = userId,
                CryptoSymbol = cryptoSymbol,
                Amount = amount,
                PriceAtPurchase = priceAtPurchase,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");   // retour au tableau
        }
        // GET  /Portfolio/Simulate
        [HttpGet]
        
        public IActionResult Simulate() => View(new NewTransactionModel());

        // POST /Portfolio/Simulate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Simulate(NewTransactionModel m, string tradeType, DateTime tradeDate)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (m.PriceAtPurchase is null or 0)
            {
                var price = await _priceService.GetLatestPrice(m.CryptoSymbol.ToUpper());
                if (price is null)
                {
                    ModelState.AddModelError("", $"Price for {m.CryptoSymbol} not found.");
                    return View(m);
                }
                m.PriceAtPurchase = price.Value;
            }

            // Quantité négative si Sell (pour faciliter le calcul)
            var qty = tradeType == "Sell" ? -Math.Abs(m.Amount) : Math.Abs(m.Amount);

            _context.Transactions.Add(new Transaction
            {
                UserId = userId,
                CryptoSymbol = m.CryptoSymbol.ToUpper(),
                Amount = qty,
                PriceAtPurchase = (decimal)m.PriceAtPurchase,
                Timestamp = tradeDate
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

    }


}
