# ADR-007 – Pluggable adatbázis provider, per-provider migrációk

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

Különböző ügyfelek eltérő adatbázis-infrastruktúrát használnak. Egyes nagyvállalatok Oracle vagy MS SQL Serverre standardizáltak, mások PostgreSQL-t preferálnak. A rendszernek kódmódosítás nélkül kell tudni váltani közöttük.

## Döntés

**EF Core provider-agnosztikus fejlesztés**, runtime provider selection konfigurációból, per-provider migráció mappákkal.

```json
// appsettings.json
{
  "Database": {
    "Provider": "PostgreSQL",
    "ConnectionString": "Host=localhost;Database=riskanalyzer;..."
  }
}
```

```csharp
// Infrastructure/DatabaseProviderExtensions.cs
public static IServiceCollection AddDatabaseProvider(
    this IServiceCollection services, IConfiguration config)
{
    var provider = config["Database:Provider"]
        ?? throw new InvalidOperationException("Database:Provider is required");
    var connectionString = config["Database:ConnectionString"]
        ?? throw new InvalidOperationException("Database:ConnectionString is required");

    return provider switch
    {
        "PostgreSQL" => services.AddNpgsql<RiskAnalyzerDbContext>(connectionString),
        "SqlServer"  => services.AddSqlServer<RiskAnalyzerDbContext>(connectionString),
        "Oracle"     => services.AddOracle<RiskAnalyzerDbContext>(connectionString),
        "Sqlite"     => services.AddSqlite<RiskAnalyzerDbContext>(connectionString),
        _ => throw new InvalidOperationException($"Unsupported database provider: {provider}")
    };
}
```

## Migráció stratégia

EF Core migrációk **nem hordozhatók** adatbázis-providerek között (különböző SQL dialektus, típusok). Megoldás: per-provider migráció mappák.

```
src/RiskAnalyzer.Infrastructure/
└── Migrations/
    ├── PostgreSQL/
    │   ├── 20260816_0001_InitialCreate.cs
    │   └── RiskAnalyzerDbContextModelSnapshot.cs
    ├── SqlServer/
    │   ├── 20260816_0001_InitialCreate.cs
    │   └── RiskAnalyzerDbContextModelSnapshot.cs
    └── Oracle/
        └── ...
```

```bash
# Migráció generálás parancsok (mind a három providerre)
dotnet ef migrations add InitialCreate --project Infrastructure \
  --context RiskAnalyzerDbContext -- --Database:Provider PostgreSQL

dotnet ef migrations add InitialCreate --project Infrastructure \
  --context RiskAnalyzerDbContext -- --Database:Provider SqlServer
```

## Provider-agnosztikus fejlesztési szabályok

- ❌ `NpgsqlDbContext` specifikus metódok tiltottak a domain/application rétegben
- ❌ Raw SQL tilos (vagy ha muszáj: provider-specifikus ágban)
- ✅ LINQ-only lekérdezések ahol lehetséges
- ✅ Provider-specifikus típusok (pl. `jsonb`) csak az Infrastructure rétegben

## Támogatott providerek

| Provider | NuGet csomag | Megjegyzés |
|---|---|---|
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | Dev default |
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` | Enterprise igény |
| Oracle | `Oracle.EntityFrameworkCore` | Licencdíjas! |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` | Csak teszteléshez |

## Következmények

- Minden sémaváltozás után mind a szükséges provider-migrációkat el kell készíteni
- CI/CD pipeline mindkét (PostgreSQL + SqlServer) migráción futtatja az integrációs teszteket
- Oracle migráció csak igény esetén kerül hozzáadásra (licenc kérdése)
- Docker Compose dev környezet: PostgreSQL default
