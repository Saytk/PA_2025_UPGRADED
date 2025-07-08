// Controllers/TradeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Models;
using System.Security.Claims;

namespace Quantia.Controllers
{
    [Authorize]
    public class TradeController : Controller
    {
        private readonly AppDbContext _context;

        public TradeController(AppDbContext context) => _context = context;

        // Utilitaire pour récupérer l’ID utilisateur courant
        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ──────────────────────────────────────────────
        // GET /Trade            → liste de tous les trades
        // ──────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId();
            var trades = await _context.Trades        // DbSet<TradeModel>
                                       .Where(t => t.UserId == userId)
                                       .OrderByDescending(t => t.BuyDate)
                                       .ToListAsync();

            return View(trades);        // Vue Trade Index créée précédemment
        }

        // ──────────────────────────────────────────────
        // GET /Trade/Create      → formulaire nouveau trade
        // ──────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create() =>
            View(new TradeModel { BuyDate = DateTime.UtcNow });

        // ──────────────────────────────────────────────
        // POST /Trade/Create     → enregistre le trade
        // ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TradeModel model)
        {

            if (!ModelState.IsValid) return View(model); // affiche les erreurs

            model.UserId = CurrentUserId();
            model.SellDate = null;
            model.SellPrice = null;
            model.BuyDate = DateTime.SpecifyKind(model.BuyDate, DateTimeKind.Utc);

            _context.Trades.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // ──────────────────────────────────────────────
        // GET /Trade/Edit/5      → éditer/fermer un trade
        // ──────────────────────────────────────────────
        //[HttpGet]
        //public async Task<IActionResult> Edit(int id)
        //{
        //    var userId = CurrentUserId();
        //    var trade = await _context.Trades
        //                               .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        //    return trade == null ? NotFound() : View(trade);
        //}

        //// ──────────────────────────────────────────────
        //// POST /Trade/Edit/5
        //// ──────────────────────────────────────────────
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, TradeModel model)
        //{
        //    var userId = CurrentUserId();
        //    var trade = await _context.Trades
        //                               .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        //    if (trade == null) return NotFound();
        //    if (!ModelState.IsValid) return View(model);

        //    // Mise à jour des champs
        //    trade.CryptoSymbol = model.CryptoSymbol;
        //    trade.Quantity = model.Quantity;
        //    trade.BuyPrice = model.BuyPrice;
        //    trade.BuyDate = model.BuyDate;
        //    trade.SellPrice = model.SellPrice;
        //    trade.SellDate = model.SellDate;

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        // ──────────────────────────────────────────────
        // POST /Trade/Delete/5
        // ──────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = CurrentUserId();
            var trade = await _context.Trades
                                       .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (trade == null) return NotFound();

            _context.Trades.Remove(trade);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET /Trade/Edit/5
        [HttpGet]
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

        // POST /Trade/Edit/5  → fermeture
        [HttpPost]
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
