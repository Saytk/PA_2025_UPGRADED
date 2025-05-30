using Microsoft.AspNetCore.Mvc;
using Quantia.Models;

namespace Quantia.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Enregistrer l'utilisateur ici (base de données, etc.)
                return RedirectToAction("Login");
            }

            return View(model);
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            // Pour l'instant, pas de validation réelle
            // Redirige directement vers le Dashboard
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
