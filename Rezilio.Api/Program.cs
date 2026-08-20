using Rezilio.Api.Middleware;
using Wolverine;
using Wolverine.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(opts =>
{
    // Story 0.3: opts.PersistMessagesWithPostgresql(...) — Wolverine outbox
    opts.Policies.AddMiddleware<ModuleAccessBehavior>(
        chain => chain.MessageType?.Namespace?.StartsWith("Rezilio") == true);
});

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddWolverineHttp();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHealthChecks("/healthz");
// Ez regisztrálja majd a Wolverine HTTP endpoint-okat (Story slice-ok)
app.MapWolverineEndpoints();
app.MapGet("/", () => Results.Ok(new { Status = "Rezilio API", Version = "0.1.0" }));

// app.Run() helyett await app.RunAsync() — Wolverine async futtatást vár
await app.RunAsync();

/*
 * Az integrációs tesztekben a WebApplicationFactory<Program> osztályt használjuk, ami a tényleges Program osztályra hivatkozik. 
 * A top-level statements-szel írt Program.cs-ben a Program osztály implicit és internal — kívülről (a tesztprojektből) nem látható. 
 * A public partial class Program; sor ezt a rejtett osztályt teszi elérhetővé a tesztprojekt számára.
 *
 * Nélküle a WebApplicationFactory<Program> fordítási hibát adna a tesztprojektben.
*/
public partial class Program;
