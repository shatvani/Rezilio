# Organization modul

## Mire való

Az Organization modul a tenant-szintű konfigurációkat kezeli. Tárolja az
alapértelmezett pénznemet, nyelvet, lokalizációs beállításokat és az időzónát.
Cross-cutting modul — nem szerepel a licenszelt `ModuleType`-ok között, ezért
a `ModuleAccessBehavior` middleware nem ellenőrzi.

## Domain

### Aggregates

**TenantSettings** — egy tenant konfigurációs beállításai:
- `TenantId` — melyik tenanthoz tartozik
- `DefaultCurrency` — alapértelmezett pénznem (`CurrencyCode` value object)
- `DefaultLanguage` — alapértelmezett nyelv (`LanguageCode` value object)
- `Locale` — lokalizációs beállítás (pl. `hu-HU`, `en-US`)
- `TimeZone` — időzóna azonosító (pl. `Central European Standard Time`, `UTC`)
- `SupportedLanguages` — a tenant által támogatott nyelvek listája (JSONB)

### Value Objects (SharedKernel)

**CurrencyCode** (`Rezilio.SharedKernel.DDD.VOs`):
- ISO 4217 validáció konstruktorban — érvénytelen kóddal `ArgumentException`
- ~150+ elfogadott kód (USD, EUR, HUF stb.)
- `implicit operator string` — közvetlenül stringként használható

**LanguageCode** (`Rezilio.SharedKernel.DDD.VOs`):
- BCP 47 regex validáció (`en`, `hu`, `en-US`, `hu-HU` stb.)
- `implicit operator string`
- Előre definiált konstansok: `LanguageCode.Hungarian`, `LanguageCode.English`

**Money** (`Rezilio.SharedKernel.DDD.VOs`):
- `Amount decimal` + `CurrencyCode`
- `+` és `-` operátorok — csak azonos pénznemben, különbözőnél `InvalidOperationException`
- ❌ Nincs árfolyam-számítás, nincs pénznemváltás

## HTTP Endpointok

| Method | Route | Leírás |
|--------|-------|--------|
| GET | `/api/organization/settings/{tenantId}` | Tenant beállítások lekérése |
| POST | `/api/organization/settings` | Beállítások létrehozása vagy frissítése (upsert) |
| POST | `/api/organization/settings/{tenantId}/languages` | Új támogatott nyelv hozzáadása |

Minden endpoint `[Authorize]` — érvényes JWT token szükséges.

## Wolverine Handlerek

### Commands

| Command | Leírás |
|---------|--------|
| `UpdateTenantSettingsCommand` | Upsert — létrehozza ha nem létezik, frissíti ha igen |
| `AddSupportedLanguageCommand` | Új nyelv hozzáadása a támogatott nyelvek listájához |

### Queries

| Query | Return type | Leírás |
|-------|-------------|--------|
| `GetTenantSettingsQuery` | `TenantSettingsResult?` | Teljes settings DTO |

## Infrastruktúra

**DbContext:** `OrganizationDbContext`  
**Tábla:** `tenant_settings`  
**SupportedLanguages tárolása:** EF Core `OwnsMany(...).ToJson()` — JSONB oszlopban,
azonos megközelítés mint a Licensing `ModuleAccesses`-nél.

**Migrations helye:** `Organization/Infrastructure/Migrations/`  
**Design-time factory:** `OrganizationDbContextFactory`

## Lokalizáció

Az API `Resources/` mappájában két JSON resource fájl van:
- `Resources/hu.json` — magyar hibaüzenetek
- `Resources/en.json` — angol hibaüzenetek

Az `Accept-Language` HTTP header alapján az ASP.NET Core
`RequestLocalizationMiddleware` választja ki a megfelelő kultúrát.
Alapértelmezett kultúra: `hu`. Támogatott: `hu`, `en`.

A hibaüzenetek localization key-ekkel hivatkozhatók `IStringLocalizer<T>`-n keresztül.

## Nevezetes Döntések

**Upsert pattern** — az `UpdateTenantSettings` handler létrehozza a settings
rekordot ha még nem létezik, különben frissíti. Ez egyszerűsíti a kliens oldalt:
nem kell külön POST/PUT logikát kezelni.

**Value objectek a SharedKernelben** — a `Money`, `CurrencyCode` és `LanguageCode`
value objectek a `SharedKernel.DDD.VOs` névtérbe kerültek, mert más modulok
(pl. RiskRegister kockázat értéke) is használni fogják őket.

**Lamar IoC** — az `OrganizationDbContext` ugyanolyan explicit
`AddSingleton(options) + AddScoped<DbContext>()` regisztrációval van ellátva
mint a `LicensingDbContext` (ADR-001).

## Függőségek

- **SharedKernel:** `AggregateRoot<T>`, `CurrencyCode`, `LanguageCode`, `Money`
- **Más moduloktól nem függ**
- **Más modulok tőle függnek:** a `TenantSettings.DefaultCurrency` az irányadó
  pénznem minden pénzügyi számításhoz — közvetlen modul-hívás helyett
  Wolverine query dispatchen keresztül kérhető le

  ## Tesztelés PowerShell-ben

```powershel
# Token (ha már megvan, kihagyható)
$token = (Invoke-RestMethod -Uri "http://localhost:8090/realms/rezilio/protocol/openid-connect/token" `
  -Method Post `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "grant_type=password&client_id=rezilio-frontend&username=dev-admin@rezilio.local&password=admin123&scope=openid").access_token

# TenantSettings lekérése
Invoke-RestMethod -Uri "http://localhost:5019/api/organization/settings/00000000-0000-0000-0000-000000000001" `
  -Headers @{ Authorization = "Bearer $token" }
```