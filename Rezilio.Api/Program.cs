using Rezilio.Api.Middleware;
using Wolverine;
using Wolverine.Http;
using Wolverine.Postgresql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A 'DefaultConnection' connection string nincs beállítva.");


builder.Host.UseWolverine(opts =>
{
    // Wolverine outbox
    // Az outbox tábla ugyanabban az adatbázisban van mint az alkalmazás adatai — így egyetlen tranzakcióba kerül a kettő:
    // BEGIN TRANSACTION
    // INSERT INTO risks(...)          ← alkalmazás adata
    // INSERT INTO wolverine_outbox(...) ← "küldendő" event
    // COMMIT
    // Nem lehetséges az az állapot, hogy az adat megvan de az event nincs — mert ha a commit előtt száll el az alkalmazás, a tranzakció visszagörget és mindkettő törlődik.
    opts.PersistMessagesWithPostgresql(connectionString);
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
