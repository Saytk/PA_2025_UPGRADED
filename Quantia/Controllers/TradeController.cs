using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Models;
using System.Security.Claims;

namespace Quantia.Controllers
{
    [Authorize]
    [Route("Trade")]
    public class TradeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICryptoPriceService _priceSvc;

        public TradeController(AppDbContext ctx, ICryptoPriceService priceSvc)
        {
            _context = ctx;
            _priceSvc = priceSvc;
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /*──────────────────────────────────────────────────────────*
         * LISTE & PnL LATENT
         *──────────────────────────────────────────────────────────*/
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var trades = await _context.Trades      // DbSet<TradeModel>
                                       .Where(t => t.UserId == CurrentUserId())
                                       .OrderByDescending(t => t.BuyDate)
                                       .ToListAsync();

            // Calcule le PnL latent pour les positions ouvertes
            foreach (var t in trades.Where(t => t.SellDate is null))
            {
                var last = await _priceSvc.GetLastPriceAsync(t.CryptoSymbol);
                if (last is null) continue;   // feed indispo

                t.UnrealizedPnl = (last.Value - t.BuyPrice) * t.Quantity;
            }

            return View(trades);   // /Views/Trade/Index.cshtml
        }

        /*──────────────────────────────────────────────────────────*
         * NOUVELLE POSITION
         *──────────────────────────────────────────────────────────*/
        [HttpGet("Create")]
        public IActionResult Create() =>
            View(new TradeModel { BuyDate = DateTime.UtcNow });

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TradeModel model)
        {
            if (!ModelState.IsValid) return View(model);

            model.UserId = CurrentUserId();
            model.Status = "Open";
            model.SellDate = null;
            model.SellPrice = null;
            model.BuyDate = DateTime.SpecifyKind(model.BuyDate, DateTimeKind.Utc);

            _context.Trades.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /*──────────────────────────────────────────────────────────*
         * FERMETURE D'UNE POSITION
         *──────────────────────────────────────────────────────────*/
        [HttpGet("Close/{id:int}")]
        public async Task<IActionResult> Close(int id)
        {
            var trade = await _context.Trades
                                       .FirstOrDefaultAsync(t => t.Id == id
                                                              && t.UserId == CurrentUserId());
            if (trade is null || trade.SellDate is not null) return NotFound();
            return View(trade);
        }

        [HttpPost("Close/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id, decimal exitPrice)
        {
            var trade = await _context.Trades
                                       .FirstOrDefaultAsync(t => t.Id == id
                                                              && t.UserId == CurrentUserId());
            if (trade is null || trade.SellDate is not null) return NotFound();

            trade.SellPrice = exitPrice;
            trade.SellDate = DateTime.UtcNow;
            trade.Status = "Closed";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /*──────────────────────────────────────────────────────────*
         * SUPPRESSION (soft : à toi de voir)
         *──────────────────────────────────────────────────────────*/
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var trade = await _context.Trades
                                       .FirstOrDefaultAsync(t => t.Id == id
                                                              && t.UserId == CurrentUserId());
            if (trade is null) return NotFound();

            _context.Trades.Remove(trade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /*──────────────────────────────────────────────────────────*
         * EDITION D'UNE POSITION
         *──────────────────────────────────────────────────────────*/
        // GET /Trade/Edit/5
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = CurrentUserId();
            var trade = await _context.Trades
                                       .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (trade == null) return NotFound();
            if (trade.Status == "Closed")   // déjà fermé : pas d’édition
                return RedirectToAction(nameof(Index));

            return View(trade);
        }

        // POST /Trade/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TradeModel model)
        {
            var userId = CurrentUserId();
            var trade = await _context.Trades
                                       .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (trade == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            // On ne modifie QUE les champs de sortie
            trade.SellPrice = model.SellPrice;
            trade.SellDate = DateTime.SpecifyKind(model.SellDate!.Value, DateTimeKind.Utc);
            trade.Status = "Closed";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}