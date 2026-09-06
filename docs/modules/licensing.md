# Licensing modul

## Mire való

A Licensing modul felelős a tenant-szintű előfizetés-kezelésért: nyilvántartja,
hogy egy adott tenant melyik **prémium modulokat** (RiskRegister, Assessment,
Treatment, Monitoring, Incidents, Compliance, Reporting, AIInsights) használhatja
aktív előfizetés vagy trial alapján, és ez alapján engedélyezi vagy tiltja le a
hozzájuk tartozó parancsokat/lekérdezéseket.

Ez a modul a REZILIO **SaaS üzleti modelljének gerince**: a rendszer egyetlen
Docker image-ként épül és települ minden ügyfélhez (ADR-003, "feature-flag
licensing"), és nem a kód, hanem a `TenantLicense` adatbázis-rekord dönti el,
mely funkciók érhetők el egy adott tenant számára. Ez a megközelítés (egy
kódbázis, konfiguráció-vezérelt funkció-elérés) jelentősen egyszerűsíti a
deploymentet és a support-ot egy hagyományos, "ügyfelenként külön build"
modellhez képest — cserébe minden prémium modul parancsának/lekérdezésének
tudnia kell, hogy licenszkötelezett-e, és ezt deklaratívan, nem elszórt
`if` ágakkal kell jeleznie (ld. lent, "Middleware").

Fontos hangsúlyozni: a Licensing modul maga **nem tud semmit** a modulok
tényleges üzleti logikájáról (mit csinál egy `Risk`, hogyan számol egy
`Assessment`) — kizárólag azt tartja nyilván, hogy *szabad-e* egyáltalán
meghívni őket egy adott tenant nevében. Ez tiszta felelősség-elválasztás:
a Licensing modul sosem fog importálni egyetlen prémium modult sem, a
prémium modulok viszont (közvetetten, egy attribútumon keresztül, ld. lent)
hivatkoznak a Licensingre.

## Domain

### Aggregates

**TenantLicense** — egy tenant teljes licensz állapota.
- `TenantId` — melyik tenanthoz tartozik
- `Plan` — előfizetési csomag (Basic / Professional / Enterprise)
- `PlanExpiresAt` — mikor jár le az előfizetés (null = örökös)
- `ModuleAccesses` — JSONB listában tárolt `ModuleAccess` value object-ek

A `Create` factory metódus a választott `Plan` alapján automatikusan
felveszi az annak megfelelő alapértelmezett modulokat (ld. lent,
`SubscriptionPlan`) `IsActive: true` állapotban. Az aggregate maga tartja
karban a `ModuleAccesses` listát: `ActivateModule`, `DeactivateModule` és
`StartTrial` mind egy belső `Replace` metódust hívnak, ami eltávolítja a
modul esetleges korábbi bejegyzését, majd felveszi az újat — ez a minta
(csere teljes rekordra, nem részleges mutáció) azért egyszerűbb és
biztonságosabb, mert a `ModuleAccess` egy `record` (immutable), tehát
"módosítani" úgysem lehetne a helyén, csak lecserélni.

### Value Objects

**ModuleAccess** — egy modul hozzáférési állapota (`sealed record`):
- `Module` — melyik `ModuleType`
- `IsActive` — adminisztratív kapcsoló (be/ki)
- `TrialEndsAt` — trial lejárata (null = nem trial)
- `IsAccessible` — computed property: `IsActive && (TrialEndsAt is null || TrialEndsAt > UtcNow)`

Az `IsAccessible` a ténylegesen használt döntési logika mindenhol (nem az
`IsActive` önmagában) — egy lejárt trial `IsActive: true` marad (senki nem
kapcsolja ki explicit), de `IsAccessible: false`, mert a `TrialEndsAt` a
múltban van. Ez a számított property az egyetlen hely, ahol ez a szabály
létezik — a hívó kódnak sosem kell magának összerakni ezt a logikát.

### Enums

**ModuleType:** RiskRegister, Assessment, Treatment, Monitoring, Incidents,
Compliance, Reporting, AIInsights

**SubscriptionPlan:**
- `Basic` — RiskRegister, Assessment, Treatment
- `Professional` — + Monitoring, Incidents
- `Enterprise` — minden modul (`Enum.GetValues<ModuleType>()`)

### Domain Events

- `ModuleActivated(TenantId, Module)` — modul aktiválásakor **és** trial
  indításakor egyaránt kiváltódik (a `StartTrial` is ezt az eventet
  emeli, nincs külön "TrialStarted" event)
- `TrialExpired(TenantId, Module)` — trial lejáratakor. **Fontos: ma
  semmi nem váltja ki ezt az eventet** — nincs olyan időzített job/
  worker, ami figyelné a lejárt trial-eket és felemelné. Az event típus
  létezik (előkészítve egy jövőbeli feldolgozáshoz, pl. emailértesítés
  vagy automatikus modul-deaktiválás), de a mögötte álló mechanizmus
  még nincs megírva.

## HTTP Endpointok

| Method | Route | Auth | Leírás |
|--------|-------|------|--------|
| GET | `/api/licensing/modules/{tenantId}` | `[Authorize]` | Aktív és hozzáférhető modulok neve (`string[]`) |
| GET | `/api/licensing/status/{tenantId}` | `[Authorize]` | Teljes licensz státusz (plan, lejárat, minden modul részletei) |
| POST | `/api/licensing/modules/{tenantId}/{module}/activate` | `[Authorize(Roles = "Admin")]` | Modul aktiválása (trial nélkül, permanens) |
| POST | `/api/licensing/modules/{tenantId}/{module}/deactivate` | `[Authorize(Roles = "Admin")]` | Modul deaktiválása |
| POST | `/api/licensing/modules/{tenantId}/{module}/start-trial` | `[Authorize(Roles = "Admin")]` | 14 napos trial indítása egy modulra |

> A `{tenantId}` route paraméter mind az öt endpointon jelen van, de a
> handlerek figyelmen kívül hagyják — lásd lent, Tenant-context enforcement.
> Ugyanezért a route-ban szereplő `tenantId` és a ténylegesen módosított/
> lekérdezett tenant **eltérhet egymástól**, ha valaki más tenant azonosítót
> ír a URL-be — ez szándékos, nem hiba: a valós tenant mindig `ITenantContext`-
> ből jön.

## Wolverine Handlerek

### Commands

| Command | Leírás |
|---------|--------|
| `CreateTenantLicenseCommand` | Új tenant licensz létrehozása adott plannel. Van hozzá `CreateTenantLicenseValidator` (kötelező `TenantId`, érvényes `Plan` enum-érték), de **nincs HTTP route-ja** — csak a dev-seed indításkori logika hívja (`Rezilio.Api/Program.cs`), tudatosan nem exponált a `story/4.16-multitenancy-phase2` epic lezárásáig (előfizetés indítása üzletileg egy fizetési/onboarding folyamat része lesz, nem egy nyílt admin endpoint) |
| `ActivateModuleCommand` | Modul aktiválása |
| `DeactivateModuleCommand` | Modul deaktiválása |
| `StartTrialCommand` | 14 napos trial indítása egy modulra |

### Queries

| Query | Return type | Leírás |
|-------|-------------|--------|
| `GetActiveModulesQuery` | `IReadOnlyList<string>` | Aktív és hozzáférhető modulok neve |
| `GetLicenseStatusQuery` | `LicenseStatusResult?` | Teljes licensz státusz DTO |

Mind a négy write handler (`Activate`/`Deactivate`/`StartTrial`,
`CreateTenantLicense` kivételével) azonos, egyszerű mintát követ: betölti
a `TenantLicense`-t `ITenantContext.TenantId` alapján, `NotFound`-ot ad
vissza, ha nincs licensz a tenanthoz, egyébként meghívja a megfelelő
aggregate-metódust és menti. Nincs FluentValidation ezekhez a parancsokhoz
— a `ModuleType` route-paraméterből érkező enum-kötés (ASP.NET model
binding) már önmagában kiszűri az érvénytelen modul-neveket egy 400-as
hibával, mielőtt a handler egyáltalán lefutna.

## Tenant-context enforcement

Minden write és read handler (`ActivateModule`, `DeactivateModule`,
`StartTrial`, `GetActiveModules`, `GetLicenseStatus`) a route-ban érkező
`{tenantId}`-t figyelmen kívül hagyja, és kizárólag az `ITenantContext.
TenantId`-val (Phase 1-ben: `FixedTenantContext`) szűr a `db.Licenses`
lekérdezésben. Ez egy audit során feltárt és javított cross-tenant IDOR-fix
(a Licensing modulra a commitot a fejlesztő véletlenül közvetlenül a
`development`-re csinálta, tudatos döntés alapján nem lett utólag
branch-re szedve, mert egyszemélyes fejlesztésben nincs PR-review igény).
A route paraméter a kliens kompatibilitása miatt maradt a szignatúrában,
de nincs ténylegesen hatása a lekérdezésre.

## Infrastruktúra

**DbContext:** `LicensingDbContext`
**Tábla:** `tenant_licenses`
**ModuleAccesses tárolása:** EF Core `OwnsMany(...).ToJson()` — a modulok listája
egyetlen `module_accesses` JSONB oszlopban van tárolva PostgreSQL-ben. Ez egyszerűsíti
a sémát és elkerüli a külön junction táblát.

**Migrations helye:** `Licensing/Infrastructure/Migrations/`
**Design-time factory:** `LicensingDbContextFactory` — az EF migration tooling
használja, hardcode-olt dev connection stringgel.

## Middleware — modul-hozzáférés ellenőrzés

**A tényleges mechanizmus attribútum-alapú, nem namespace-konvenció.** A
`Rezilio.Modules.Licensing` névtérben lévő `[RequiresModule(ModuleType.X)]`
attribútum tehető rá egy Command/Query **osztályra**, jelezve, hogy a
végrehajtásához az adott tenantnek aktív licensszel kell rendelkeznie az
`X` modulra. A `Rezilio.Api` projektben lévő `ModuleAccessBehavior`
Wolverine pipeline middleware minden üzenet előtt lefut, és:

1. reflection-nel megnézi, van-e a beérkező üzenet típusán
   `RequiresModuleAttribute` — ha nincs (ez a mai helyzet **minden**
   Organization command/query esetén, hiszen az a modul szándékosan nincs
   licenszelve), egyszerűen továbbengedi;
2. ha van, reflection-nel megkeresi az üzenet `TenantId` nevű property-jét,
   és ha nem talál olvasható `Guid` értéket, egy figyelmeztetést logol és
   **szintén továbbengedi** (fail-open, nem fail-closed — ez tudatos
   választás volt, hogy egy hiányzó `TenantId` property ne blokkolja le a
   teljes rendszert egy fejlesztői hiba miatt, de ez azt is jelenti, hogy
   egy elgépelt property-név csendben kikapcsolja a védelmet);
3. az `IModuleAccessChecker.IsModuleActiveAsync(module, tenantId)`
   hívással ellenőrzi a licenszet, és `ModuleNotLicensedException`-t dob,
   ha nem aktív.

**Ez a mechanizmus ma még sehol nincs ténylegesen bevetve** — egyetlen
meglévő Command/Query osztályon (Organization vagy Licensing) sincs
`[RequiresModule]` attribútum, mert a licenszköteles modulok
(RiskRegister és társai) még nem léteznek. A middleware és az attribútum
előre elkészített infrastruktúra, amit az első prémium modul story-ja
(`story/1.2-risk-commands`) fog először ténylegesen használni.

> ⚠️ **Fontos architekturális figyelmeztetés a jövőbeli moduloknak:** a
> 2. pontban leírt `TenantId`-kiolvasás **magáról a Command/Query
> objektumról** történik reflection-nel — vagyis pontosan arról a
> mezőről, amiben az Organization és a Licensing modul handlerei
> tudatosan **nem** bíznak meg (ld. "Tenant-context enforcement" fent és
> az Organization modul dokumentációjában). Ha egy jövőbeli RiskRegister
> command spoofolt `TenantId`-val érkezik, a licensz-ellenőrzés a hamis
> tenant licenszét fogja megnézni — miközben a tényleges handlernek
> (ha helyesen van megírva) a valós, `ITenantContext`-ből jövő tenantra
> kellene írnia/olvasnia. A két ellenőrzés emiatt **elméletileg
> szétcsúszhat**: egy támadó, akinek nincs RiskRegister licensze,
> elméletben megpróbálhatja a saját command-ján egy másik (RiskRegister-
> licenszes) tenant azonosítóját feltüntetni, hogy átjusson a
> `ModuleAccessBehavior`-on — de utána a handler a saját (licensz nélküli)
> tenantjára írna, ami funkcionálisan használhatatlan neki, tehát ez
> önmagában nem adatszivárgás, "csak" egy logikailag nem tiszta állapot.
> Ennek ellenére **erősen javasolt**, hogy amikor az első `[RequiresModule]`
> attribútum ténylegesen felkerül egy Command-ra, a `ModuleAccessBehavior`
> is átkerüljön `ITenantContext`-alapú olvasásra a reflection helyett —
> ezt érdemes lesz felvenni nyitott kérdésként a RiskRegister 3. lépcsős
> (API/slice) tervébe.

## Nevezetes Döntések

**Lamar IoC container** (ADR-001): A Microsoft DI helyett Lamar-t használunk,
mert a Wolverine handler code generation az EF Core `AddDbContext` lambda
regisztrációját nem tudta feloldani (`ServiceLocationPolicy.NotAllowed`).
Lamar mélyebb DI introspection-nel natívan kezeli ezt. A csere minimálisan
invazív — Lamar teljes IServiceCollection kompatibilitást nyújt.

> Érdekesség: a Licensing modul saját regisztrációja (`AddLicensingModule`)
> a sima `services.AddDbContext<LicensingDbContext>(...)` hívást
> használja, míg az Organization modulnak a fent említett code-gen hiba
> miatt egy kézzel épített `DbContextOptionsBuilder` + `AddSingleton` +
> `AddScoped<Context>` mintára kellett váltania (ld.
> `docs/modules/organization.md`, "Infrastruktúra"). A Licensing modulban
> ez a hiba (ma) nem jelentkezett — valószínűleg azért, mert kevesebb és
> egyszerűbb Wolverine HTTP handlere van. Ha egy jövőbeli modulnál
> (RiskRegister) ismét felbukkanna ez a code-gen probléma, az Organization
> mintáját érdemes követni, nem a Licensingét.

**Self-registering modul:** A Licensing modul saját `AddLicensingModule()`
extension methoddal regisztrálja a saját service-eit. A `Program.cs` csak
ezt hívja meg — nem tudja a modul belső felépítését. Ez a minta minden
modulnál (Organization, és a jövőbeli RiskRegister/Assessment/stb.)
egységesen visszatér — ez biztosítja, hogy az `Api` host projekt sosem
kényszerül modul-specifikus DI-huzalozásra, csak egy-egy
`AddXModule(connectionString)` hívásra.

**Attribútum-alapú, nem namespace-konvenciós licenszellenőrzés:** a
tervezés során felmerült egy egyszerűbb alternatíva is (a command
namespace-éből — pl. `.Modules.RiskRegister.` — kitalálni a modult
string-egyezés alapján), de ez törékenyebb lett volna: egy elgépelt vagy
átszervezett namespace csendben eltörné az ellenőrzést, futásidejű hiba
nélkül. Az explicit `[RequiresModuleAttribute]` ezzel szemben fordítási
időben látható, kereshető, és a fejlesztőnek tudatos döntést kell hoznia
minden egyes Command/Query esetén, hogy licenszköteles-e.

## Tervezett bővítés (még nem implementálva)

A `ComplianceFramework` katalógus (lásd `docs/ADR-016-compliance-framework-
catalog.md` és `SPEC.md` 3.5. szakasz) tervezetten a `TenantLicense`
aggregate-et egy második, a `ModuleAccesses`-hez hasonló gyűjteménnyel
bővíti majd (`FrameworkAccesses`), hogy az egyes megfelelőségi
keretrendszerek (NIS2, DORA...) önállóan, licenszszinten legyenek
be-/kikapcsolhatók. Ez a `story/3.11-compliance-api` story része lesz,
jelen dokumentum a kódban ma ténylegesen létező állapotot írja le.

## Függőségek

- **SharedKernel:** `AggregateRoot<T>`, `DomainEvent`, `ITenantContext`,
  `IClaimsTransformation`, `AppClaims`
- **Más moduloktól nem függ** — a többi modul viszont közvetve függ tőle
  a `[RequiresModule]` attribútumon és a `ModuleAccessBehavior`
  middleware-en keresztül (ld. fent)

## Tesztelés PowerShell-ben

```powershell
# Token (ha mar megvan, kihagyhato)
$token = (Invoke-RestMethod -Uri "http://localhost:8090/realms/rezilio/protocol/openid-connect/token" `
-Method Post `
-ContentType "application/x-www-form-urlencoded" `
-Body "grant_type=password&client_id=rezilio-frontend&username=dev-admin@rezilio.local&password=admin123&scope=openid").access_token

# Aktiv modulok lekerese
Invoke-RestMethod -Uri "http://localhost:5019/api/licensing/modules/00000000-0000-0000-0000-000000000001" `
-Headers @{ Authorization = "Bearer $token" }
```
