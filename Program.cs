using BusTicketing.Data;
using BusTicketing.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// 1. Add Localization Services
// ---------------------------------------------------------
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources"; // All .resx files go inside /Resources/
});

// ---------------------------------------------------------
// 2. Add MVC + View & DataAnnotation Localization
// ---------------------------------------------------------
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// ---------------------------------------------------------
// 3. EF Core DbContext
// ---------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------
// 4. Other Services
// ---------------------------------------------------------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<SmsService>();

var app = builder.Build();

// ---------------------------------------------------------
// 5. Localization Configuration (English + French)
// ---------------------------------------------------------
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("fr")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Enable culture providers (URL → Cookie → Browser)
localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new QueryStringRequestCultureProvider(),   // ?culture=fr
    new CookieRequestCultureProvider(),        // cookie
    new AcceptLanguageHeaderRequestCultureProvider() // browser
};

app.UseRequestLocalization(localizationOptions);

// ---------------------------------------------------------
// 6. App Middleware Pipeline
// ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();

// ---------------------------------------------------------
// 7. Routing
// ---------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
