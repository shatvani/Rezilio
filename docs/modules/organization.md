# Organization modul

## Mire való

Az Organization modul a tenant szervezeti referencia-/törzsadatait és a
tenant-szintű konfigurációt kezeli. Ez az egyetlen **mindig aktív, nem
licenszelt** modul — nem szerepel a `ModuleType` enumban, ezért a
Licensing modul `ModuleAccessBehavior` middleware-je (lásd
`docs/modules/licensing.md`) rá nézve soha nem fut le érdemben: az
Organization parancsain/lekérdezésein egyszerűen nincs feltéve
`[RequiresModule]` attribútum, tehát minden tenant korlátozás nélkül
hozzáfér. Ez tudatos üzleti döntés — a szervezeti alapadat (kik vagyunk,
mink van, ki miért felelős) nélkül a rendszer semelyik prémium modulja
(RiskRegister, Assessment stb.) nem tudna értelmesen működni, ezért ez
"ingyenes alapkőnek" számít, nem eladható funkciónak.

Két fő felelőssége van:

1. **Szervezeti törzsadatok** — szervezeti egységek, ügyfelek, beszállítók,
   kulcsszemélyek, IT rendszerek, üzleti folyamatok, ezek Excel importja
2. **Tenant-szintű konfiguráció** — alapértelmezett pénznem, nyelv, locale,
   időzóna, támogatott nyelvek listája

Architekturálisan ez a modul a **referencia-gerinc**: a leendő RiskRegister
modul kockázatai `KeyPerson`-re (tulajdonos), `ItSystem`-re/
`BusinessProcess`-re/`Supplier`-re (érintett eszköz) fognak hivatkozni. Mivel
a REZILIO moduláris monolit (ADR-001), ezek a hivatkozások **sosem** lesznek
közvetlen FK-relációk adatbázis-szinten a modulhatáron át — csak
alkalmazás-szintű `Guid` hivatkozások, amiket a hivatkozó modul saját
FluentValidation validátora ellenőriz le (lásd lent, "FluentValidation
lefedettség" — az `ItSystem`/`BusinessProcess` `OwnerId` mezője már ma is
pontosan ezt a mintát követi a `KeyPerson`-nel szemben).

## Domain

### Aggregates

**OrganizationalUnit** — szervezeti egység (SZMSZ hierarchia):
`TenantId`, `Name`, `Code` (trim + uppercase normalizált), `ParentId?`
(önhivatkozó hierarchia — nincs kényszerítve ciklusmentesség kód szinten,
ez a frontend fa-nézet felelőssége lesz), `Description?`

**Customer** — ügyfél: `TenantId`, `Name`, `Code` (normalizált),
`Industry?`, `Country?`, `ContactEmail?`, `ContactPhone?`, `Description?`

**Supplier** — beszállító: ugyanaz a mezőkészlet mint `Customer`-nél.
Ez az entitás lesz a jövőbeli **harmadik fél/beszállítói kockázat (Third-
Party Risk Management)** kapcsolódási pontja is, ahogy azt a
`docs/design/risk-register-design.md` 1.9-es szakasza tárgyalja.

**KeyPerson** — kulcsszemély: `TenantId`, `Name`, `Code` (normalizált —
lásd Nevezetes Döntések), `Title?`, `Department?`, `OrgUnitId?`, `Email?`,
`Phone?`, `BackupPersonName?`, `Description?`

**ItSystem** — IT rendszer: `TenantId`, `Code`, `Name`, `Type`
(`ItSystemType`), `Vendor?`, `Version?`, `HostingType`, `OwnerId?`
(→ `KeyPerson`), `CriticalityLevel`, `SupportedOrgUnitIds` (JSONB tömb).
`Create` egy `ItSystemCreated` domain eventet vált ki.

**BusinessProcess** — üzleti folyamat: `TenantId`, `Code`, `Name`,
`OwnerId?` (→ `KeyPerson`), `OrgUnitId?`, `Category`, `CriticalityLevel`,
`MaxTolerableDowntimeMinutes?`, `RecoveryTimeObjectiveMinutes?`,
`DependsOnSystemIds` (JSONB tömb). `Create` egy `BusinessProcessCreated`
domain eventet vált ki.

> **Üzleti szabály:** `Create`/`Update` elutasítja a műveletet, ha
> `RecoveryTimeObjectiveMinutes > MaxTolerableDowntimeMinutes` — ennek
> üzletileg nincs értelme (a helyreállítási céling nem lehet hosszabb,
> mint amit a folyamat még elvisel). Ez az egyetlen aggregate ebben a
> modulban, ami ezt a szabályt **`Result`/`Result<T>` visszatéréssel**
> (`Rezilio.SharedKernel.Results`) jelzi kivétel helyett — minden más
> aggregate itt vagy `ArgumentException`-t dob (pl. üres név esetén), vagy
> egyáltalán nem validál domain szinten (a FluentValidation validátorra
> bízza). Ez egy tudatosan **nem egységes** minta a modulon belül — érdemes
> lesz eldönteni, hogy a jövőbeli modulok (RiskRegister) melyiket
> kövessék alapértelmezettként (a `Result` minta jobban skálázódik több,
> egyszerre jelezhető hibára, de több boilerplate-et igényel).

**ImportJob** — egy Excel import életciklusa: `TenantId`, `EntityType`,
`Status` (`ImportJobStatus` státuszgép), `TotalRows`, `SuccessRows`,
`ErrorRows`, `CreatedAt`, `CompletedAt?`, `FileContent` (byte[] — maga a
feltöltött fájl, hogy a `ConfirmImport` a validált tartalmat dolgozza fel),
`Results` (soronkénti `ImportRowResult` lista, JSONB)

Státuszgép: `Pending → Validating → Valid/Invalid → Importing → Completed`,
bármely aktív állapotból `Failed` (`EnsureStatus` védi az érvénytelen
átmeneteket, `InvalidOperationException`-t dob). `Create`, `Complete` és
`Fail` egyaránt domain eventet vált ki (`ImportJobCreated`,
`ImportJobCompleted`, `ImportJobFailed`) — ez a modul egyetlen aggregate-je,
aminek minden állapotváltása eseményesített.

**TenantSettings** — tenant konfiguráció: `TenantId`, `DefaultCurrency`
(`CurrencyCode`), `DefaultLanguage` (`LanguageCode`), `Locale`, `TimeZone`,
`SupportedLanguages` (JSONB lista) — `Create` a `DefaultLanguage`-et
automatikusan felveszi a `SupportedLanguages`-be is, `Update` és
`AddSupportedLanguage` dedupe-olva bővíti.

> **Domain event megjegyzés:** jelenleg csak az `ItSystem` és a
> `BusinessProcess` aggregate vált ki domain eventet létrehozáskor
> (`ItSystemCreated`, `BusinessProcessCreated`) — az `OrganizationalUnit`,
> `Customer`, `Supplier`, `KeyPerson` és `TenantSettings` nem. Ennek ma
> nincs funkcionális következménye (semmi nem iratkozik fel ezekre az
> eventekre), de érdemes lesz tudatosan eldönteni, hogy ez a hiányosság,
> vagy szándékos (pl. csak azok az entitások kapnak eventet, amikre egy
> jövőbeli modulnak — mondjuk a RiskRegisternek — ténylegesen fel kell
> iratkoznia).

### Value Objects (SharedKernel — `Rezilio.SharedKernel.DDD.VOs`)

**CurrencyCode** — ISO 4217 validáció konstruktorban (~150 elemű
whitelist), `ArgumentException` érvénytelen kódra, `implicit operator
string`

**LanguageCode** — BCP 47 regex validáció (`^[a-zA-Z]{2,3}(-[a-zA-Z0-9]
{2,8})*$`), `ArgumentException` érvénytelen formátumra, `implicit operator
string`, előre definiált konstansok: `LanguageCode.Hungarian`,
`LanguageCode.English`

**Money** — `Amount decimal` + `CurrencyCode`, `+`/`-` csak azonos
pénznemben, eltérőnél `InvalidOperationException`. Nincs
árfolyam-számítás. Ez a value object lesz később a leendő kvantitatív
kockázatbecslés (ld. `docs/design/risk-register-design.md` 1.4) alapja is.

### Enums

**EntityType:** OrganizationalUnit, Location, Customer, Supplier,
KeyPerson, ItSystem, BusinessProcess

> A `Location` szerepel az enumban, de a `Location` aggregate maga még
> nincs implementálva (`TASKS.md` Story ORG.4 — todo). Ez azt jelenti,
> hogy az Excel import motor és a `ColumnDefinitionProvider`-ek elméletileg
> fel vannak készítve rá, de a ténylegesen importálható/kezelhető
> entitások köre ma hattal (nem hét) egyenlő.

**ImportJobStatus:** Pending, Validating, Valid, Invalid, Importing,
Completed, Failed

**CriticalityLevel:** Low, Medium, High, Critical

**HostingType:** OnPrem, Cloud, Hybrid

**ItSystemType:** Erp, Crm, Hrm, Bi, ECommerce, Infrastructure, Security,
Communication, Other

## HTTP Endpointok

Minden endpoint `[Authorize]` — érvényes JWT token szükséges, nincs
role-alapú megszorítás (a törzsadat-szerkesztés minden hitelesített
felhasználónak elérhető — szemben a Licensing modul admin-műveleteivel,
amik `[Authorize(Roles = "Admin")]`-lal vannak védve).

| Method | Route | Leírás |
|--------|-------|--------|
| GET | `/api/organization/units` | Szervezeti egységek listája (tenant szerint) |
| GET | `/api/organization/units/{id}` | Egy szervezeti egység |
| POST | `/api/organization/units` | Létrehozás |
| PUT | `/api/organization/units/{id}` | Módosítás |
| DELETE | `/api/organization/units/{id}` | Törlés |
| GET | `/api/organization/customers` | Ügyfelek listája |
| GET | `/api/organization/customers/{id}` | Egy ügyfél |
| POST | `/api/organization/customers` | Létrehozás |
| PUT | `/api/organization/customers/{id}` | Módosítás |
| DELETE | `/api/organization/customers/{id}` | Törlés |
| GET | `/api/organization/suppliers` | Beszállítók listája |
| GET | `/api/organization/suppliers/{id}` | Egy beszállító |
| POST | `/api/organization/suppliers` | Létrehozás |
| PUT | `/api/organization/suppliers/{id}` | Módosítás |
| DELETE | `/api/organization/suppliers/{id}` | Törlés |
| GET | `/api/organization/key-persons` | Kulcsszemélyek listája |
| GET | `/api/organization/key-persons/{id}` | Egy kulcsszemély |
| POST | `/api/organization/key-persons` | Létrehozás |
| PUT | `/api/organization/key-persons/{id}` | Módosítás |
| DELETE | `/api/organization/key-persons/{id}` | Törlés |
| GET | `/api/organization/it-systems` | IT rendszerek listája |
| GET | `/api/organization/it-systems/{id}` | Egy IT rendszer |
| POST | `/api/organization/it-systems` | Létrehozás |
| PUT | `/api/organization/it-systems/{id}` | Módosítás |
| DELETE | `/api/organization/it-systems/{id}` | Törlés |
| GET | `/api/organization/business-processes` | Üzleti folyamatok listája |
| GET | `/api/organization/business-processes/{id}` | Egy üzleti folyamat |
| POST | `/api/organization/business-processes` | Létrehozás |
| PUT | `/api/organization/business-processes/{id}` | Módosítás |
| DELETE | `/api/organization/business-processes/{id}` | Törlés |
| GET | `/api/organization/import/{entityType}/template` | Excel sablon letöltése |
| POST | `/api/organization/import/{entityType}/upload` | Fájl feltöltés + validáció → `ImportJob` |
| GET | `/api/organization/import/{importJobId}/status` | Import job státusza + összesítő számok |
| GET | `/api/organization/import/{importJobId}/results` | Soronkénti eredmények (opcionális `?errorsOnly=true`) |
| POST | `/api/organization/import/{importJobId}/confirm` | Validált import véglegesítése |
| GET | `/api/organization/settings/{tenantId}` | Tenant beállítások lekérése |
| POST | `/api/organization/settings` | Beállítások upsert |
| POST | `/api/organization/settings/{tenantId}/languages` | Új támogatott nyelv hozzáadása |

## Tenant-context enforcement

Minden fenti handler a query/route paraméterben vagy Command-ban érkező
`TenantId`-t figyelmen kívül hagyja, és kizárólag az `ITenantContext.
TenantId`-t (Phase 1-ben: `FixedTenantContext`, mindig a fix dev tenant)
használja szűrésre és létrehozáskor. Ez egy utólagos, audit során feltárt
és javított cross-tenant IDOR-fix (`fix/org-tenant-context-enforcement`) —
a kliens oldali Command/Query DTO-k mezője megmaradt (a frontend
kompatibilitása miatt), csak a szerver nem bízik meg az értékében.

**Miért fontos ezt itt kiemelni:** a Licensing modul jövőbeli
`[RequiresModule]`-alapú licensz-ellenőrzése (lásd `docs/modules/
licensing.md`, "Middleware") a Command/Query saját `TenantId` mezőjét
olvassa ki reflection-nel — vagyis *azt*, amiben itt tudatosan **nem**
bízunk meg adathozzáférés szempontjából. Ha egy jövőbeli, licenszkötelezett
modul (pl. RiskRegister) parancsa spoofolt `TenantId`-val érkezik, a
licensz-ellenőrzés a hamis tenant licenszét nézné meg, de maga a handler —
ha ugyanezt a mintát követi, mint az Organization modul — a valós
(`ITenantContext`) tenantra írna/olvasna. Ez két külön védelmi réteg, és
mindkettőt helyesen kell megvalósítani a jövőbeli moduloknál.

## Excel import motor

**Könyvtár:** ClosedXML (MIT licensz) — az EPPlus alternatívájaként azért
esett erre a választás, mert az EPPlus 5-től kezdve kereskedelmi licenszet
igényel üzleti használatra (NonCommercial License), míg a ClosedXML MIT
alatt marad, és a REZILIO-nak nincs szüksége az EPPlus fejlettebb (pl.
pivot tábla, VBA-makró) funkcióira — csak egyszerű celladat-olvasásra/
-írásra és alapvető formázásra (fejléc, dropdown validáció a sablonban).

Folyamat: `GET .../template` → sablon letöltés (fejléc, kötelező mezők,
dropdown validációk) → `POST .../upload` → soronkénti validáció, `ImportJob`
létrehozása `Validating` állapotban, majd `Valid`/`Invalid`-ra zárás →
`GET .../status` és `GET .../results` az eredmény megtekintéséhez →
`POST .../confirm` a tényleges adatbetöltés, csak `Valid` állapotú jobból.

Az `ImportJob.FileContent` mezőben a teljes feltöltött fájl tárolódik, hogy
a `ConfirmImport` handler a már validált bájtokból dolgozzon, ne kelljen
újra feltölteni. Ennek van egy tárolási ára is (a fájl bájtjai bekerülnek a
`import_jobs` táblába), amit érdemes szem előtt tartani, ha nagy volumenű
importok válnak jellemzővé — egy jövőbeli optimalizáció lehet a fájl
ideiglenes blob-tárolóba (pl. S3-kompatibilis object storage) helyezése a
sima DB-oszlop helyett, de ez ma nem indokolt a várható fájlméretek
mellett (a UI is 5000 sorban maximalizálja az elfogadott import méretét,
lásd `rezilio-web/TASKS.md` Story ORG.10 megszorítás).

A modul minden entitástípushoz külön `IImportColumnDefinitionProvider`
implementációt regisztrál (`OrganizationalUnitColumnDefinitionProvider`,
`CustomerColumnDefinitionProvider` stb.) — ez a **stratégia minta**
(strategy pattern) egy tiszta alkalmazása: az `ExcelTemplateGenerator` és
az `ExcelImportParser` maga entitás-agnosztikus, csak a providerektől kért
oszlop-metaadatok (név, kötelező-e, típus, dropdown-forrás) alapján épít
sablont, illetve validál egy feltöltött fájlt. Új entitástípus bevezetése
így csak egy új provider megírását igényli, a generátor/parser kódját nem
kell módosítani.

## Wolverine Handlerek

Minden entitáshoz (OrganizationalUnit, Customer, Supplier, KeyPerson,
ItSystem, BusinessProcess) azonos mintájú öt slice tartozik: `Create*`,
`Update*`, `Delete*` Command + Handler, `Get*ById`, `Get*sByTenant` Query +
Handler. Import-specifikus handlerek: `UploadAndValidateImportHandler`,
`ConfirmImportHandler`, `GetImportJobStatusHandler`,
`GetImportJobResultsHandler`, `DownloadImportTemplateHandler`.
Settings-specifikus: `UpdateTenantSettingsHandler` (upsert),
`AddSupportedLanguageHandler`, `GetTenantSettingsHandler`.

### FluentValidation lefedettség

| Entitás | Validator megvan? |
|---|---|
| Customer | Create + Update |
| Supplier | Create + Update |
| KeyPerson | Create + Update |
| ItSystem | Create + Update — **+ aszinkron `OwnerId` → `KeyPerson` FK-ellenőrzés** (`MustAsync`, tenant-hez kötve) |
| BusinessProcess | Create + Update — **+ ugyanaz az aszinkron `OwnerId` → `KeyPerson` FK-ellenőrzés** |
| **OrganizationalUnit** | **Nincs Create/Update validator (Fix ORG.F2 — todo)** |
| TenantSettings | Update |
| AddSupportedLanguage | van |

A `Code`-egyediség (409 Conflict) előzetes ellenőrzése viszont már minden
Create handlerben megvan, beleértve `OrganizationalUnit`-ot is — csak a
mezőszintű FluentValidation hiányzik onnan.

**Az `OwnerId` FK-ellenőrzés mintája** (`ItSystem`/`BusinessProcess`)
jelenti a modul egyetlen konkrét példáját arra, hogyan validál egy
entitás egy *másik* entitásra mutató hivatkozást ugyanazon modulon belül:
a validátor a `OrganizationDbContext`-et és az `ITenantContext`-et injektálja,
és egy `MustAsync` szabállyal ellenőrzi, hogy a megadott `OwnerId` egy
létező, **ugyanahhoz a tenanthoz tartozó** `KeyPerson` rekordra mutat-e.
Ez a minta lesz az irányadó a leendő `RiskRegister` modul `Risk.OwnerId`
mezőjéhez is — azzal a különbséggel, hogy ott a `KeyPerson` egy **másik
modul** entitása lesz, tehát a validátor nem az `OrganizationDbContext`-et,
hanem egy Organization modul által exponált, erre a célra dedikált
lekérdezést (pl. egy `IKeyPersonLookup` service-t) fog használni — közvetlen
`OrganizationDbContext` injektálás egy másik modul Application/
Infrastructure rétegébe megsértené a modulhatárokat (ADR-001).

## Infrastruktúra

**DbContext:** `OrganizationDbContext`
**Táblák:** `organizational_units`, `customers`, `suppliers`,
`key_persons`, `it_systems`, `business_processes`, `import_jobs`,
`tenant_settings`

**JSONB oszlopok (`OwnsMany(...).ToJson()`):**
- `TenantSettings.SupportedLanguages` → `supported_languages`
- `ImportJob.Results` → `results`
- `ItSystem.SupportedOrgUnitIds`, `BusinessProcess.DependsOnSystemIds` →
  natív `jsonb` oszlop típus (`HasColumnType("jsonb")`, nem owned collection)

**Egyediség (unique index, tenant szinten):** `(TenantId, Code)` minden
`Code`-dal rendelkező entitáson (OrganizationalUnit, Customer, Supplier,
KeyPerson). Emellett minden entitás kap egy önálló `HasIndex(e =>
e.TenantId)` indexet is — ez a napi lekérdezési minta (mindig egy adott
tenant listáját kérjük le) teljesítményét szolgálja, függetlenül attól,
hogy szűrünk-e `Code`-ra is.

**Modul-regisztráció (`OrganizationModule.AddOrganizationModule`):** a
`DbContextOptionsBuilder` + `AddSingleton(options)` + `AddScoped<Context>`
minta (nem a megszokott `services.AddDbContext<T>(...)` egysoros hívás)
azért kellett, mert a Wolverine handler code generation nem tudta feloldani
az `AddDbContext` belső lambda-regisztrációját (lásd ADR-000/ADR-001 a
Lamar IoC bevezetéséről). **Érdekesség:** a Licensing modul
(`LicensingModule.AddLicensingModule`) ugyanerre a problémára nem ütközött,
és egyszerű `services.AddDbContext<LicensingDbContext>(...)`-et használ —
ez egy megfigyelt inkonzisztencia a két modul között, aminek az oka
valószínűleg az, hogy a Licensing modulban (ma) nincs olyan Wolverine
HTTP handler, ami a `DbContext`-et közvetlenül, generált kódból épített
konstruktor-paraméterként várná ugyanolyan módon, mint az Organization
import-handlerei. Ha egy jövőbeli modulnál (RiskRegister) hasonló
code-gen hiba jelentkezne, az Organization mintáját (nem a Licensingét)
érdemes követni.

**Migrations helye:** `Organization/Infrastructure/Migrations/`
**Design-time factory:** `OrganizationDbContextFactory`

## Lokalizáció

`Resources/hu.json` / `Resources/en.json` — `Accept-Language` header
alapján az ASP.NET Core `RequestLocalizationMiddleware` választja ki a
kultúrát. Alapértelmezett: `hu`. Támogatott: `hu`, `en`.

## Ismert hiányosságok

- **`OrganizationalUnit`-nak nincs FluentValidation validátora** — a
  többi öt entitás mind kapott validátort a `fix/org-validation-hardening`
  körben (`TASKS.md` Fix ORG.F1), de az `OrganizationalUnit` kimaradt
  belőle. Nyomon követve: `TASKS.md` Fix ORG.F2.
- **`OrganizationalUnit.ParentId` ciklus-ellenőrzés nélküli** — a domain
  modell nem akadályozza meg, hogy egy egység saját magának (közvetve)
  őse legyen. Ma ez elméleti kockázat (nincs UI, ami ilyet könnyen
  létrehozna), de ha a frontend fa-szerkesztő (ORG.10, backlog tétel)
  drag-and-drop-pal engedi az áthelyezést, ezt validálni kell majd.
- **Domain event lefedettség egyenetlen** — ld. fent, "Domain event
  megjegyzés" az Aggregates szakaszban.

## Nevezetes Döntések

**Upsert pattern a Settings-nél** — `UpdateTenantSettings` létrehozza a
rekordot, ha még nincs, egyébként frissíti.

**Code normalizálás** — minden `Code` mezővel rendelkező entitás
(`OrganizationalUnit`, `Customer`, `Supplier`, `KeyPerson`) a `Create`/
`Update` metódusban `.Trim().ToUpperInvariant()`-tal normalizálja a kódot.
Ennek gyakorlati oka van: az Excel import és a kézi UI-bevitel is
emberi gépelésből származik, ahol a "abc-01" és "ABC-01" ugyanazt az üzleti
kódot jelenti — normalizálás nélkül ez két külön, "egyedi" rekordként
csúszna be az adatbázisba, és az `(TenantId, Code)` unique index sem
védene ellene. A `KeyPerson.Code` mezőt utólag, egy audit során vezettük
be (`fix/org-keyperson-code-field`) — korábban az import owner-matching
név alapján történt, ami pontatlan volt (két azonos nevű, de különböző
személy összekeveredhetett).

**Value objectek a SharedKernelben** — `Money`, `CurrencyCode`,
`LanguageCode` azért kerültek a `SharedKernel.DDD.VOs` névtérbe, mert más
modulok (pl. leendő RiskRegister) is használni fogják őket. Ez egy
tudatos kompromisszum a moduláris monolit elve (ADR-001, modulok ne
függjenek egymástól) és a kód-duplikáció elkerülése között: a
`SharedKernel` egy közös, minden modul által látott projekt, de csak
technikai/domain-agnosztikus building block-okat tartalmaz (VO-k,
`AggregateRoot<T>`, `DomainEvent`) — sosem üzleti logikát vagy
modul-specifikus entitást.

## Függőségek

- **SharedKernel:** `AggregateRoot<T>`, `CurrencyCode`, `LanguageCode`,
  `Money`, `ITenantContext`, `Result`/`Result<T>` (`BusinessProcess`
  aggregate-ben)
- **Más moduloktól nem függ**
- **Más modulok tőle függnek majd:** a leendő `RiskRegister` modul
  `Risk.OwnerId` mezője egy meglévő `KeyPerson`-ra fog mutatni (Handler
  szintű validációval, az `ItSystem`/`BusinessProcess` `OwnerId`
  mintáját követve), és a `Supplier`/`ItSystem`/`BusinessProcess`
  entitások lesznek a kockázatok "mihez tartozik" referenciái is.

## Tesztelés PowerShell-ben

```powershell
$token = (Invoke-RestMethod -Uri "http://localhost:8090/realms/rezilio/protocol/openid-connect/token" `
  -Method Post `
  -ContentType "application/x-www-form-urlencoded" `
  -Body "grant_type=password&client_id=rezilio-frontend&username=dev-admin@rezilio.local&password=admin123&scope=openid").access_token

# Ugyfelek listaja
Invoke-RestMethod -Uri "http://localhost:5019/api/organization/customers" `
  -Headers @{ Authorization = "Bearer $token" }

# Tenant beallitasok
Invoke-RestMethod -Uri "http://localhost:5019/api/organization/settings/00000000-0000-0000-0000-000000000001" `
  -Headers @{ Authorization = "Bearer $token" }
```
