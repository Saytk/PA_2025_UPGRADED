using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Quantia.Controllers
{
    public class TradeHistoryController : Controller
    {
        private readonly AppDbContext _context;

        public TradeHistoryController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var closedTrades = await _context.Trades
                .Where(t => t.UserId == userId && t.Status == "Closed")
                .OrderByDescending(t => t.SellDate)
                .ToListAsync();

            return View(closedTrades);   // envoie la liste à la vue
        }

    }
}
