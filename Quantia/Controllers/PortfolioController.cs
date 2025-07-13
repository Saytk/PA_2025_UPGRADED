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
    [Route("Portfolio")]
    public class PortfolioController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PortfolioPriceService _price;

        public PortfolioController(AppDbContext ctx, PortfolioPriceService priceService)
        {
            _context = ctx;
            _price = priceService;
        }

        /* ───────────── TABLAU PnL (vue Razor) ────────────────────────── */
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rows = await _context.Transactions
                                     .Where(t => t.UserId == userId)
                                     .ToListAsync();

            var grouped = rows.GroupBy(t => t.CryptoSymbol)
                              .Select(g => new {
                                  Symbol = g.Key,
                                  Qty = g.Sum(t => t.Amount),
                                  Invested = g.Sum(t => t.Amount * t.PriceAtPurchase)
                              });

            var table = new List<PortfolioRow>();
            foreach (var g in grouped)
            {
                var last = await _price.GetLatestPrice(g.Symbol);
                if (last is null) continue;

                var curVal = g.Qty * last.Value;
                table.Add(new PortfolioRow
                {
                    Symbol = g.Symbol,
                    Quantity = g.Qty,
                    Invested = g.Invested,
                    CurrentPrice = last.Value,
                    CurrentValue = curVal,
                    PnL = curVal - g.Invested
                });
            }
            return View(table);
        }

        /* ───────────── ENDPOINT JSON : stats agrégées ─────────────────── */
        [HttpGet("Stats")]
        public async Task<IActionResult> Stats()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var rows = await _context.Transactions
                                     .Where(t => t.UserId == userId)
                                     .ToListAsync();

            if (!rows.Any()) return Json(new PortfolioStats());

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

            var stats = new PortfolioStats
            {
                Balance = balance,
                UnrealizedPnL = balance - invested,
                WinRate = wins + losses == 0 ? 0 : wins / (wins + losses),
                ProfitFactor = losses == 0 ? wins : wins / losses
            };
            return Json(stats);
        }

        /* ───────────── ENDPOINT JSON : courbe equity ──────────────────── */
        [HttpGet("Equity")]
        public async Task<IActionResult> Equity(int days = 30)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
                // utilisation du prix historique ; fallback au prix d'achat
                var price = await _price.GetHistoricalPrice(tx.CryptoSymbol, tx.Timestamp)
                            ?? tx.PriceAtPurchase;

                balance += tx.Amount * price;
                dates.Add(tx.Timestamp);
                equity.Add(balance);
            }
            return Json(new { dates, equity });
        }

        /* ───────────── Ajout transaction via formulaire Create ────────── */
        [HttpGet("Create")]
        public IActionResult Create() => View(new NewTransactionModel());

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewTransactionModel m)
        {
            if (!ModelState.IsValid) return View(m);

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (m.PriceAtPurchase is null or 0)
            {
                var p = await _price.GetLatestPrice(m.CryptoSymbol.ToUpper());
                if (p is null)
                {
                    ModelState.AddModelError("", $"Price for {m.CryptoSymbol} not found.");
                    return View(m);
                }
                m.PriceAtPurchase = p.Value;
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

        /* ───────────── Ajout JSON depuis dashboard Prediction ─────────── */
        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] TxDto dto)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            _context.Transactions.Add(new Transaction
            {
                UserId = userId,
                CryptoSymbol = dto.CryptoSymbol.ToUpper(),
                Amount = dto.Amount,
                PriceAtPurchase = dto.PriceAtPurchase,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return Ok(new { status = "saved" });
        }

        public record TxDto(string CryptoSymbol, decimal Amount, decimal PriceAtPurchase);

        /* ───────────── Formulaire simulate (Buy / Sell daté) ───────────── */
        [HttpGet("Simulate")]
        public IActionResult Simulate() => View(new NewTransactionModel());

        [HttpPost("Simulate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Simulate(NewTransactionModel m, string tradeType, DateTime tradeDate)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (m.PriceAtPurchase is null or 0)
            {
                var p = await _price.GetLatestPrice(m.CryptoSymbol.ToUpper());
                if (p is null)
                {
                    ModelState.AddModelError("", $"Price for {m.CryptoSymbol} not found.");
                    return View(m);
                }
                m.PriceAtPurchase = p.Value;
            }

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
