using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Quantia.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserModel user)
        {
            if (!ModelState.IsValid)
                return View(user);

            var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                return View(user);
            }

            // ✅ Hasher le mot de passe ici
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            // Déconnexion automatique si un utilisateur est déjà connecté
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync("QuantiaAuth");
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginModel login)
        {
            if (!ModelState.IsValid)
                return View(login);

            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
            if (userInDb == null || !BCrypt.Net.BCrypt.Verify(login.Password, userInDb.Password))
            {
                ModelState.AddModelError(string.Empty, "Email ou mot de passe invalide.");
                return View(login);
            }

            // Création des claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userInDb.Id.ToString()),
                new Claim(ClaimTypes.Name, userInDb.LastName),
                new Claim(ClaimTypes.Email, userInDb.Email)
            };

            // Construction de l’identité
            var identity = new ClaimsIdentity(claims, "QuantiaAuth");
            var principal = new ClaimsPrincipal(identity);

            // Connexion via cookies
            await HttpContext.SignInAsync("QuantiaAuth", principal);

            // Redirection post-login
            return RedirectToAction("Index", "Dashboard");

        }

        public IActionResult Index()
        {
            return View();
        }
    }

}
