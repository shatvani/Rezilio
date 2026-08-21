# Licensing modul

## Mire való

A Licensing modul felelős a tenant-szintű előfizetés-kezelésért. Nyilvántartja,
hogy egy adott tenant melyik modulokat használhatja (aktív előfizetés vagy trial
alapján), és minden bejövő Wolverine command/query esetén ellenőrzi a jogosultságot
a pipeline middleware-en keresztül.

## Domain

### Aggregates

**TenantLicense** — egy tenant teljes licensz állapota.
- `TenantId` — melyik tenanthoz tartozik
- `Plan` — előfizetési csomag (Basic / Professional / Enterprise)
- `PlanExpiresAt` — mikor jár le az előfizetés (null = örökös)
- `ModuleAccesses` — JSONB listában tárolt `ModuleAccess` value object-ek

### Value Objects

**ModuleAccess** — egy modul hozzáférési állapota:
- `IsActive` — adminisztratív kapcsoló (be/ki)
- `TrialEndsAt` — trial lejárata (null = nem trial)
- `IsAccessible` — computed: `IsActive && (TrialEndsAt is null || TrialEndsAt > UtcNow)`

### Enums

**ModuleType:** RiskRegister, Assessment, Treatment, Monitoring, Incidents,
Compliance, Reporting, AIInsights

**SubscriptionPlan:**
- `Basic` — RiskRegister, Assessment, Treatment
- `Professional` — + Monitoring, Incidents
- `Enterprise` — minden modul

### Domain Events

- `ModuleActivated(TenantId, Module)` — modul aktiválásakor és trial indításakor
- `TrialExpired(TenantId, Module)` — trial lejáratakor (jövőbeli feldolgozáshoz)

## HTTP Endpointok

| Method | Route | Leírás |
|--------|-------|--------|
| GET | `/api/licensing/modules/{tenantId}` | Visszaadja az aktív modulok neveit (`string[]`) |
| GET | `/api/licensing/status/{tenantId}` | Teljes licensz státusz (plan, lejárat, minden modul részletei) |

Mindkét endpoint `[Authorize]` — érvényes JWT token szükséges.

## Wolverine Handlerek

### Commands

| Command | Leírás |
|---------|--------|
| `CreateTenantLicenseCommand` | Új tenant licensz létrehozása adott plannel |
| `ActivateModuleCommand` | Modul aktiválása (trial nélkül, permanens) |
| `DeactivateModuleCommand` | Modul deaktiválása |
| `StartTrialCommand` | 14 napos trial indítása egy modulra |

### Queries

| Query | Return type | Leírás |
|-------|-------------|--------|
| `GetActiveModulesQuery` | `IReadOnlyList<string>` | Aktív és hozzáférhető modulok neve |
| `GetLicenseStatusQuery` | `LicenseStatusResult?` | Teljes licensz státusz DTO |

## Infrastruktúra

**DbContext:** `LicensingDbContext`  
**Tábla:** `tenant_licenses`  
**ModuleAccesses tárolása:** EF Core `OwnsMany(...).ToJson()` — a modulok listája
egyetlen `module_accesses` JSONB oszlopban van tárolva PostgreSQL-ben. Ez egyszerűsíti
a sémát és elkerüli a külön junction táblát.

**Migrations helye:** `Licensing/Infrastructure/Migrations/`  
**Design-time factory:** `LicensingDbContextFactory` — az EF migration tooling
használja, hardcode-olt dev connection stringgel.

## Middleware — ModuleAccessBehavior

A `Rezilio.Api` projektben lévő `ModuleAccessBehavior` Wolverine pipeline
middleware minden Rezilio command/query előtt lefut (kivéve a Licensing saját
üzenetei). Namespace-konvenció alapján határozza meg a modult
(pl. `.Modules.RiskRegister.` → `ModuleType.RiskRegister`), majd az
`IModuleAccessChecker` service-en keresztül ellenőrzi a tenant licenszét.
Ha a modul nincs aktiválva, `ModuleNotLicensedException` kivételt dob.

## Nevezetes Döntések

**Lamar IoC container** (ADR-001): A Microsoft DI helyett Lamar-t használunk,
mert a Wolverine handler code generation az EF Core `AddDbContext` lambda
regisztrációját nem tudta feloldani (`ServiceLocationPolicy.NotAllowed`).
Lamar mélyebb DI introspection-nel natívan kezeli ezt. A csere minimálisan
invazív — Lamar teljes IServiceCollection kompatibilitást nyújt.

**Self-registering modul:** A Licensing modul saját `AddLicensingModule()`
extension methoddal regisztrálja a saját service-eit. A `Program.cs` csak
ezt hívja meg — nem tudja a modul belső felépítését.

## Függőségek

- **SharedKernel:** `AggregateRoot<T>`, `DomainEvent`, `ITenantContext`,
  `IClaimsTransformation`, `AppClaims`
- **Más moduloktól nem függ** — a többi modul viszont közvetve függ tőle
  a `ModuleAccessBehavior` middleware-en keresztül

## Tesztelés PowerShell-ben

```powershel
# Token (ha már megvan, kihagyható)
$token = (Invoke-RestMethod -Uri "http://localhost:8090/realms/rezilio/protocol/openid-connect/token" `
-Method Post `
-ContentType "application/x-www-form-urlencoded" `
-Body "grant_type=password&client_id=rezilio-frontend&username=dev-admin@rezilio.local&password=admin123&scope=openid").access_token

# Aktív modulok lekérése
Invoke-RestMethod -Uri "http://localhost:5019/api/licensing/modules/00000000-0000-0000-0000-000000000001" `
-Headers @{ Authorization = "Bearer $token" }
```