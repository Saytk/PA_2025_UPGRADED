using Microsoft.EntityFrameworkCore;
using Quantia.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────
// DATABASE & SERVICES
// ──────────────────────────────

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddControllersWithViews();

// Services personnalisés
builder.Services.AddSingleton<SentimentFileService>();
builder.Services.AddHttpClient<PortfolioPriceService>();

// Sessions & cookies
builder.Services.AddSession();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = CookieSecurePolicy.Always;
});

// ──────────────────────────────
// AUTHENTIFICATION COOKIE
// ──────────────────────────────

builder.Services.AddAuthentication("QuantiaAuth")
    .AddCookie("QuantiaAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Anti-CSRF
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ──────────────────────────────
// HTTP CLIENTS
// ──────────────────────────────

// Client générique (par défaut)
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PortfolioEquityService>();

// Client ML vers API Python
builder.Services.AddHttpClient("MLApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");  // Port de FastAPI
});

// Client local ASP.NET (self-call vers /Portfolio/Equity, etc.)
builder.Services.AddHttpClient("LocalAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:7248");  // Port ASP.NET
});

// ──────────────────────────────
// BUILD & PIPELINE
// ──────────────────────────────

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCookiePolicy();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Route MVC par défaut
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();
