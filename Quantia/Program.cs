using Microsoft.EntityFrameworkCore;
using Quantia.Services;
using Quantia.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────
// DATABASE & SERVICES
// ──────────────────────────────

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddControllersWithViews();

// Services personnalisés
builder.Services.AddScoped<ISentimentRepository, EfSentimentRepository>();
builder.Services.AddScoped<SentimentService>();
builder.Services.AddScoped<PortfolioEquityService>();
builder.Services.AddHttpClient<TradeSuggestionService>();
builder.Services.AddScoped<TradeSuggestionService>();


// Sessions
builder.Services.AddSession();

// ──────────────────────────────
// AUTHENTIFICATION & COOKIES
// ──────────────────────────────

if (builder.Environment.IsProduction())
{
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        options.Secure = CookieSecurePolicy.SameAsRequest;
    });

    builder.Services.AddAuthentication("QuantiaAuth")
        .AddCookie("QuantiaAuth", options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
}
else
{
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        options.Secure = CookieSecurePolicy.Always;
    });

    builder.Services.AddAuthentication("QuantiaAuth")
        .AddCookie("QuantiaAuth", options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    builder.Services.AddAntiforgery(options =>
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
}

// ──────────────────────────────
// HTTP CLIENTS
// ──────────────────────────────

// Client générique
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PortfolioEquityService>();

// Client ML vers API Python
builder.Services.AddHttpClient("MLApi", client =>
{
    var baseUrl = builder.Environment.IsProduction() 
        ? "https://api-test-049u.onrender.com"  // URL Kubernetes
        : "http://localhost:8000";      // URL développement
    client.BaseAddress = new Uri(baseUrl);
});

// Client local ASP.NET
builder.Services.AddHttpClient("LocalAPI", client =>
{
    var baseUrl = builder.Environment.IsProduction()
        ? "http://localhost:8080"       // Port interne Kubernetes
        : "http://localhost:7248";      // Port développement
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICryptoPriceService, CryptoPriceService>();
builder.Services.AddScoped<PortfolioPriceService>();

// ──────────────────────────────
// BUILD & PIPELINE
// ──────────────────────────────

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// COMMENTÉ temporairement pour validation SSL
// app.UseHttpsRedirection();

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

// ──────────────────────────────
// SUPPORT HEAD REQUESTS POUR SSL
// ──────────────────────────────
// Nécessaire pour Google Managed SSL Certificate validation
//app.MapMethods("/", new[] { "GET", "HEAD" }, async context => 
//{
//    if (context.Request.Method == "HEAD")
//    {
//        context.Response.StatusCode = 200;
//        return;
//    }
//    // Pour GET, rediriger vers la route par défaut (Account/Login)
//    context.Response.Redirect("/Account/Login");
//});

// Support pour les challenges ACME (validation SSL)
//app.MapMethods("/.well-known/acme-challenge/{*path}", new[] { "GET", "HEAD" }, async context =>
//{
//    context.Response.StatusCode = 200;
//    if (context.Request.Method == "GET")
//    {
//        await context.Response.WriteAsync("OK");
//    }
//});

app.Run();
