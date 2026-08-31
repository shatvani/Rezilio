using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;                          // ← ez kell a MigrateAsync + AnyAsync-hoz
using Rezilio.Api.Middleware;
using Rezilio.Modules.Licensing;
using Rezilio.Modules.Licensing.Domain;                      // ← ez kell a TenantLicense, ModuleType, stb.-hez
using Rezilio.Modules.Licensing.Infrastructure;
using Rezilio.Modules.Organization;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.DDD.VOs;
using Rezilio.SharedKernel.Multitenancy;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseLamar();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A 'DefaultConnection' connection string nincs beállítva.");

builder.Host.UseWolverine(opts =>
{
    // Wolverine outbox: alkalmazás adat + event egyetlen tranzakcióban
    opts.PersistMessagesWithPostgresql(connectionString);

    // FluentValidation automatikus 400 Bad Request-té alakítása
    opts.UseFluentValidation();

    // Licensing assembly handlereinek beregisztrálása
    opts.Discovery.IncludeAssembly(typeof(LicensingModule).Assembly);
    opts.Discovery.IncludeAssembly(typeof(OrganizationModule).Assembly);

    opts.Policies.AddMiddleware<ModuleAccessBehavior>(
        chain => chain.MessageType?.Namespace?.StartsWith("Rezilio") == true
              && chain.MessageType.Namespace.Contains(".Modules.Licensing.") == false);
});

// Auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false; // csak dev-ben!
    });
builder.Services.AddAuthorization();

// Keycloak claim-eket normalizálja AppClaims konstansokra
builder.Services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformation>();

// Multitenancy – Phase 1: egyetlen fix TenantId
builder.Services.AddScoped<ITenantContext, FixedTenantContext>();

// ModuleNotLicensedException → 403 ProblemDetails
builder.Services.AddExceptionHandler<ModuleNotLicensedExceptionHandler>();
builder.Services.AddProblemDetails();

// CORS – a frontend (Next.js, jellemzően más porton fut) böngészős hívásaihoz.
// Az engedélyezett origin-eket konfigurációból olvassuk (appsettings.Development.json:
// Cors:AllowedOrigins), hogy környezetenként (dev/éles) eltérő legyen, kódmódosítás nélkül.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

// --- Licensing ---
builder.Services.AddLicensingModule(connectionString);
// --- Organization ---
builder.Services.AddOrganizationModule(connectionString);
// --- Lokalizáció ---
builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("Frontend");

// Dev módban automatikus migráció – production-ban kézi migráció
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    // Licensing migráció
    var db = scope.ServiceProvider.GetRequiredService<LicensingDbContext>();
    await db.Database.MigrateAsync();

    // Organization migráció
    var orgDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
    await orgDb.Database.MigrateAsync();

    // Dev seed – Enterprise licensz minden modullal
    var devTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    bool exists = await db.Licenses.AnyAsync(l => l.TenantId == devTenantId);
    if (!exists)
    {
        var license = TenantLicense.Create(devTenantId, SubscriptionPlan.Enterprise);
        foreach (var module in Enum.GetValues<ModuleType>())
        {
            license.ActivateModule(module);
        }
        db.Licenses.Add(license);
        await db.SaveChangesAsync();
    }

    // Organization dev seed
    bool settingsExist = await orgDb.TenantSettings.AnyAsync(s => s.TenantId == devTenantId);
    if (!settingsExist)
    {
        var settings = TenantSettings.Create(
            devTenantId,
            new CurrencyCode("HUF"),
            new LanguageCode("hu"),
            locale: "hu-HU",
            timeZone: "Central European Standard Time");
        orgDb.TenantSettings.Add(settings);
        await orgDb.SaveChangesAsync();
    }

    // TODO Story 1.1: RisksDbContext migráció
}

app.UseHealthChecks("/healthz");

var supportedCultures = new[] { "hu", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("hu")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseAuthentication();
app.UseAuthorization();
app.MapWolverineEndpoints();
app.MapGet("/", () => Results.Ok(new { Status = "Rezilio API", Version = "0.1.0" }));

await app.RunAsync();

public partial class Program;
