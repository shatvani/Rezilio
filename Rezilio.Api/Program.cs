using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Rezilio.Api.Middleware;
using Rezilio.SharedKernel.Multitenancy;
using Wolverine;
using Wolverine.Http;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A 'DefaultConnection' connection string nincs beállítva.");

builder.Host.UseWolverine(opts =>
{
    // Wolverine outbox: alkalmazás adat + event egyetlen tranzakcióban
    opts.PersistMessagesWithPostgresql(connectionString);
    opts.Policies.AddMiddleware<ModuleAccessBehavior>(
        chain => chain.MessageType?.Namespace?.StartsWith("Rezilio") == true);
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

var app = builder.Build();

// Dev módban automatikus migráció – production-ban kézi migráció
if (app.Environment.IsDevelopment())
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    // TODO Story 1.1: modulonként futtatni a migrációkat, ha lesz DbContext
    // await scope.ServiceProvider.GetRequiredService<RiskDbContext>().Database.MigrateAsync();
}

app.UseHealthChecks("/healthz");
app.UseAuthentication();
app.UseAuthorization();
app.MapWolverineEndpoints();
app.MapGet("/", () => Results.Ok(new { Status = "Rezilio API", Version = "0.1.0" }));

await app.RunAsync();

public partial class Program;
