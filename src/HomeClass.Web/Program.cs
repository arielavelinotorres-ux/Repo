using System.Globalization;
using HomeClass.Application;
using HomeClass.Domain.Entities;
using HomeClass.Infrastructure;
using HomeClass.Infrastructure.Persistence;
using HomeClass.Infrastructure.Seed;
using HomeClass.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection' en appsettings.json.");

// ---- Capas de la aplicación ----
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

// ---- Identidad: las cuentas las crea la administración, no hay registro público ----
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<DescriptorErroresEs>()
    .AddClaimsPrincipalFactory<AppClaimsFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HomeClass.Auth";
    options.Cookie.HttpOnly = true;
    options.LoginPath = "/Cuenta/Ingresar";
    options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Si la administración desactiva una cuenta, la sesión abierta deja de ser válida en menos de un minuto.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromSeconds(30));

// ---- MVC: todos los POST validan el token anti-falsificación ----
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var seed = builder.Configuration.GetSection("Seed").Get<SeedOptions>() ?? new SeedOptions();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Cultura fija es-NI: fechas en español y punto decimal, tanto al mostrar como al recibir números de los formularios.
var cultura = new CultureInfo("es-NI");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Panel}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.InicializarAsync(app.Services, seed);

app.Run();
