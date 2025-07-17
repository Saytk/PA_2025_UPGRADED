using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Quantia.Data;
using Quantia.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// ──────────────────────────────
// HTTP CLIENTS Nommés
// ──────────────────────────────
builder.Services.AddHttpClient("MLApi", client =>
{
    client.BaseAddress = new Uri("https://api-test-049u.onrender.com");
});
builder.Services.AddHttpClient("LocalAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:7248");
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