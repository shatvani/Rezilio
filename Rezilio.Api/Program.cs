using Lamar.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;                          // ← ez kell a MigrateAsync + AnyAsync-hoz
using Rezilio.Api.Middleware;
using Rezilio.Modules.Licensing;
using Rezilio.Modules.Licensing.Domain;                      // ← ez kell a TenantLicense, ModuleType, stb.-hez
using Rezilio.Modules.Licensing.Infrastructure;
using Rezilio.SharedKernel.Multitenancy;
using Wolverine;
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

    // Licensing assembly handlereinek beregisztrálása
    opts.Discovery.IncludeAssembly(typeof(LicensingModule).Assembly);

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

builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

builder.Services.AddLicensingModule(connectionString);

var app = builder.Build();

// Dev módban automatikus migráció – production-ban kézi migráció
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<LicensingDbContext>();

    await db.Database.MigrateAsync();

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
    // TODO Story 1.1: RisksDbContext migráció
}

app.UseHealthChecks("/healthz");
app.UseAuthentication();
app.UseAuthorization();
app.MapWolverineEndpoints();
app.MapGet("/", () => Results.Ok(new { Status = "Rezilio API", Version = "0.1.0" }));

await app.RunAsync();

public partial class Program;
