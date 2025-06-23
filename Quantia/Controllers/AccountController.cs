using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;

namespace Quantia.Controllers
{
    public class AccountController : Controller
    {
        // --- constante pour éviter les "magical strings" ---
        private const string AuthScheme = "QuantiaAuth";

        private readonly AppDbContext _context;
        public AccountController(AppDbContext context) => _context = context;

        /* ---------- INSCRIPTION ---------- */

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(UserModel user)
        {
            if (!ModelState.IsValid) return View(user);

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Cet email est déjà utilisé.");
                return View(user);
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        /* ---------- CONNEXION ---------- */

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                await HttpContext.SignOutAsync(AuthScheme);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel login)
        {
            if (!ModelState.IsValid) return View(login);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
            {
                ModelState.AddModelError(string.Empty, "Email ou mot de passe invalide.");
                return View(login);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name,           user.LastName),
                new Claim(ClaimTypes.Email,          user.Email)
            };

            await HttpContext.SignInAsync(
                AuthScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, AuthScheme)));

            return RedirectToAction("Index", "Dashboard");
        }

        public IActionResult Index() => View();

        /* ---------- LOGIN FACTICE POUR LE DEV ---------- */
#if DEBUG
        [HttpGet("/dev-login")]
        [AllowAnonymous]
        public async Task<IActionResult> DevLogin()
        {
            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment()) return NotFound();

            var fakeClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "0"),
                new Claim(ClaimTypes.Name,           "DevUser"),
                new Claim(ClaimTypes.Email,          "dev@example.com")
            };

            await HttpContext.SignInAsync(
                AuthScheme,
                new ClaimsPrincipal(new ClaimsIdentity(fakeClaims, AuthScheme)));

            return RedirectToAction("Index", "Dashboard");
        }
#endif
    }
}
