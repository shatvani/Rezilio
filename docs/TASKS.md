# Risk Analyzer – Feladatlista (TASKS.md)

> **Státusz jelölések:** `[ ]` Todo · `[~]` In Progress · `[x]` Done · `[!]` Blocker  
> **Utolsó frissítés:** 2026-08-19  
> **Branch névképzési konvenció:** lásd `docs/CLAUDE.md` → Branch névképzési stratégia  
> **Verziókezelés:** SemVer 2.0, CHANGELOG.md – lásd `docs/ADR-014-versioning.md`

---

## EPIC 0 – Alapinfrastruktúra

> **Cél:** Futó skeleton, auth, licensz-ellenőrzés. Minden más erre épül.  
> **Időtartam:** ~4 hét (2 sprint)

---

### EPIC-0 / Sprint 1 – Backend alap

#### Story 0.1 – Solution és projekt struktúra
> **Branch:** `story/0.1-solution-structure`  
> Moduláris monolith skeleton felállítása, konvenciók rögzítése

- [x] Solution létrehozása (`RiskAnalyzer.sln`)
- [x] Projekt struktúra: `src/RiskAnalyzer.Api`, `src/RiskAnalyzer.SharedKernel`, `src/Modules/`
- [x] `SharedKernel` alap osztályok: `AggregateRoot<TId>`, `DomainEvent`, `ValueObject`
- [x] `TenantId` value object létrehozása (Phase 1: fix érték)
- [x] `Result<T>` pattern implementálása (hibakezeléshez)
- [x] `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props` beállítása

**Elfogadási kritériumok:**
- `dotnet build` hibamentesen lefut, nulla warning
- Az API projekt elindul (`dotnet run`) és 200-as választ ad a `/health` endpointon
- `AggregateRoot<TId>`, `ValueObject`, `Result<T>` unit tesztekkel lefedve (legalább boldog-út)
- Az `.editorconfig` szabályok CI-ban ellenőrzöttek (Roslyn analyzer)

**Megszorítások:**
- Nincs tényleges üzleti logika – csak infrastruktúra és alap osztályok
- `TenantId` Phase 1-ben egyetlen hardcoded konstans érték (multitenancy még nem aktív)

---

#### Story 0.2 – Wolverine konfiguráció
> **Branch:** `story/0.2-wolverine-setup`  
> Wolverine message bus alap beállítása

- [x] Wolverine NuGet csomag hozzáadása
- [x] `WolverineOptions` alapkonfiguráció az API projektben
- [x] `ModuleAccessBehavior` middleware váz (licensz-ellenőrzés pipeline-ba)
- [x] Első szinkron Command + Handler működő példával validálva

**Elfogadási kritériumok:**
- Egy demó Command (pl. `PingCommand`) + Handler sikeresen lefut Wolverine dispatch-en keresztül
- `ModuleAccessBehavior` regisztrálva a pipeline-ban (még nem ellenőriz, de be van kötve)
- Wolverine belső naplózás megjelenik dev-ben (console)

**Megszorítások:**
- Async üzenetküldés (queue, durable outbox) nem kerül konfigurálásra ebben a story-ban

---

#### Story 0.3 – EF Core + PostgreSQL alap
> PostgreSQL fix provider, Wolverine outbox, TenantContext alap

- [x] `Npgsql.EntityFrameworkCore.PostgreSQL` csomag hozzáadása
- [x] `WolverineFx.Postgresql` csomag hozzáadása
- [x] `ConnectionStrings:DefaultConnection` konfiguráció (`appsettings.json` + `appsettings.Development.json`)
- [x] Wolverine outbox bekötése (`opts.PersistMessagesWithPostgresql(...)`)
- [x] `ITenantContext` interface definiálása (`SharedKernel`-ben)
- [x] `FixedTenantContext` implementáció (Phase 1: fix `TenantId.Default`)
- [x] `FixedTenantContext` DI regisztrációja az API projektben

**Elfogadási kritériumok:**
- Az alkalmazás elindul, az EF Core migráció lefut, az adatbázis létrejön
- Egy mintaentitáson (pl. `AuditLog`) CRUD műveletek elvégezhetők a DbContext-en keresztül
- A `ITenantContext` injektálható egy Handler-be, és visszaadja a fix TenantId-t

**Megszorítások:**
- ❌ Nincs `IDatabaseProvider` absztrakció, nincs `AddDatabaseProvider()` extension method
- ❌ Nincs provider-specifikus migráció mappa (pl. `Migrations/SqlServer/`)
- ❌ Nincs MS SQL, Oracle, SQLite NuGet csomag

---

#### Story 0.4 – Docker Compose (dev)
> **Branch:** `story/0.4-docker-compose-dev`  
> Lokális fejlesztői környezet containerizálva

- [x] `docker-compose.dev.yml`: API + PostgreSQL + Keycloak service-ek
- [x] `.env.example` fájl az összes szükséges változóval (értékek nélkül)
- [x] Health check endpoint az API-n (`/health`)
- [x] Adatbázis automatikus migráció induláskor (dev módban)
- [x] `README.md` – lokális futtatási útmutató

**Elfogadási kritériumok:**
- `docker compose -f docker-compose.dev.yml up` egyetlen paranccsal elindítja az összes service-t
- Az API `/health` endpointja 200-at ad vissza a containerből
- Egy dev felhasználóval be lehet jelentkezni Keycloak-ba
- A README alapján egy új fejlesztő 15 percen belül el tudja indítani a dev környezetet

**Megszorítások:**
- ❌ A `rezilio-realm-dev.json` soha nem tartalmaz production secret-et
- ❌ A `.env` fájl soha nem kerül verziókezelésbe – csak `.env.example`

---

### EPIC-0 / Sprint 2 – Identity + Licensing alap

#### Story 0.5 – Keycloak OIDC integráció
> **Branch:** `story/0.5-keycloak-oidc`  
> JWT validáció és claims normalizálás – Keycloak az egyetlen IdP (ADR-012)

- [x] `Modules/Identity/` struktúra felállítása
- [x] Keycloak JWT middleware konfiguráció az API-ban (OpenID Connect, `rezilio` realm)
- [x] `keycloak/rezilio-realm-dev.json` — dev realm, user-ek, role-ok, client konfig
- [x] Keycloak service hozzáadása `docker-compose.yml`-be
- [x] `AppClaims` statikus konstansok (`app:user_id`, `app:tenant_id`, `app:email`, `app:roles`)
- [x] `KeycloakClaimsTransformation : IClaimsTransformation` implementáció
  - `sub` → `AppClaims.UserId`
  - `email` → `AppClaims.Email`
  - `tenant_id` → `AppClaims.TenantId`
  - `realm_access.roles` → `AppClaims.Roles`
- [ ] `GetCurrentUser` Query + Handler
- [x] Role-based authorization policy-k regisztrálása (`Admin`, `RiskManager`, `RiskOwner`, `Auditor`, `Executive`, `Viewer`)
- [x] Keycloak dev realm JSON frissítve az összes role-lal

**Elfogadási kritériumok:**
- Érvényes Keycloak JWT tokennel védett endpoint elérhető, érvénytelen tokennel 401-et ad
- A Keycloak Admin Console elérhető (`http://localhost:8080`)
- `GetCurrentUser` Handler visszaadja az `AppClaims` alapú felhasználói adatokat
- `[Authorize(Roles = "Admin")]` attribútum helyes szerepkörrel 200-at, nem megfelelővel 403-at ad
- Handler-ekben közvetlenül `AppClaims` konstansok használatosak (nem Keycloak-specifikus claim nevek)
- Unit teszt: `KeycloakClaimsTransformation` helyes claim mapping-et végez

**Megszorítások:**
- ❌ Nincs LocalSql, AzureEntraID, ActiveDirectory auth provider implementáció
- ❌ Nincs `RegisterUser` / `LoginUser` Command – a felhasználókezelés Keycloak feladata
- ❌ Nincs jelszó tárolva a REZILIO adatbázisban

---

#### Story 0.6 – Licensing modul alap
> **Branch:** `story/0.6-licensing-module`  
> Tenant licensz kezelése, modul-hozzáférés ellenőrzése

- [x] `Modules/Licensing/` struktúra felállítása
- [x] `TenantLicense` aggregate, `ModuleAccess` value object
- [x] `ModuleType` enum (minden modul felsorolva)
- [x] `SubscriptionPlan` enum (Basic, Professional, Enterprise)
- [x] `CreateTenantLicense` Command + Handler
- [x] `GetLicenseStatus` Query
- [x] `GetActiveModules` Query
- [x] `CheckModuleAccess` – szinkron belső service (middleware hívja)
- [x] `ActivateModule` / `DeactivateModule` Command
- [x] `StartTrial` Command (14 napos trial logika)
- [x] `ModuleActivated` / `TrialExpired` domain event-ek

**Elfogadási kritériumok:**
- Deaktivált modulhoz tartozó endpoint `403 Forbidden` választ ad `ModuleNotLicensedException`-nel
- Aktív modul esetén a kérés átmegy az ellenőrzésen
- Trial létrehozás után 14 napig aktív a modul, lejárat után inaktív
- `ModuleAccessBehavior` Wolverine middleware-be kötve (minden Command/Query automatikusan ellenőrzött)
- Unit teszt: `TenantLicense` invariáns-védelem (pl. lejárt licenszre nem lehet modult aktiválni)

**Megszorítások:**
- A licensz ellenőrzés szinkron, in-process – nincs külső licensz-szerver
- Phase 1-ben egyetlen tenant van, de a licensz adatmodell multitenancy-ready

---

#### Story 0.7 – TenantSettings + Pénznem + Lokalizáció (backend)
> **Branch:** `story/0.7-tenant-settings`  
> Cross-cutting konfigurációk backend oldala

- [x] `TenantSettings` aggregate (DefaultCurrency, DefaultLanguage, SupportedLanguages, Locale, TimeZone)
- [x] `Money` value object (`Amount decimal` + `CurrencyCode ISO 4217`)
- [x] `CurrencyCode` value object (ISO 4217 validáció)
- [x] `LanguageCode` value object (BCP 47 validáció)
- [x] `GetTenantSettings` Query + Handler
- [x] `UpdateTenantSettings` Command + Handler
- [x] `AddSupportedLanguage` Command + Handler
- [x] `IStringLocalizer<T>` konfiguráció, JSON resource fájlok (`Resources/en.json`, `Resources/hu.json`)
- [x] Backend hibaüzenetek lokalizálva (minden `Result` error message localization key)

**Elfogadási kritériumok:**
- `Money` value object: összeadás/kivonás azonos pénznemben működik, eltérő pénznemben exception-t dob
- `CurrencyCode("XYZ")` érvénytelen kóddal `ValidationException`-t dob
- Hibaüzenetek `hu` és `en` lokalizáción is visszaadódnak (Accept-Language header alapján)
- `TenantSettings` update után a módosított értékek visszaolvashatók

**Megszorítások:**
- ❌ Nincs pénznemváltás / árfolyam-számítás – csak azonos devizás műveletek
- ❌ Ne égess be pénznem kódot a kódba – mindig `TenantSettings.DefaultCurrency`

---

#### Story 0.8 – Next.js projekt alap + i18n
> **Branch:** `story/0.8-nextjs-base`  
> Frontend projekt inicializálása, auth flow, többnyelvűség

- [ ] Next.js 14+ projekt létrehozása (App Router)
- [ ] TanStack Query konfiguráció
- [ ] `next-intl` konfiguráció (middleware, `i18n.ts`, locale routing)
- [ ] `messages/en.json` – alap struktúra (common, errors, modulonként szekciók)
- [ ] `messages/hu.json` – magyar fordítás
- [ ] Nyelv váltó komponens (felhasználói preferencia mentéssel)
- [ ] Auth context (Keycloak OIDC redirect flow, JWT tárolás httpOnly cookie-ban)
- [ ] Login / Logout oldal (Keycloak login page redirect)
- [ ] Protected route wrapper
- [ ] API kliens alap (`lib/api-client.ts`)
- [ ] "Upgrade szükséges" komponens (deaktivált modulhoz, lokalizálva)
- [ ] Tailwind CSS konfiguráció ellenőrzése (`tailwind.config.ts`, `globals.css`)
- [ ] shadcn/ui inicializálása (`npx shadcn@latest init`)
- [ ] Alap shadcn/ui komponensek telepítése: `Button`, `Card`, `Badge`, `Separator`

**Elfogadási kritériumok:**
- Bejelentkező gomb Keycloak login page-re navigál, sikeres login után visszairányít az alkalmazásba
- Védett route-ra nem autentikált felhasználó nem juthat el (redirect login-ra)
- Nyelv váltáskor az összes feliraton azonnal megjelenik a fordítás (oldal újratöltés nélkül)
- `lib/api-client.ts` automatikusan csatolja a JWT tokent az Authorization headerbe
- TypeScript hibák nélkül forduljon le

**Megszorítások:**
- ❌ Nincs JWT tárolás localStorage-ban – csak httpOnly cookie vagy memory
- ❌ Nincs közvetlen `fetch()` hívás a komponensekben – `lib/api-client.ts` kell
- ❌ Nincs hardcoded UI szöveg – minden `useTranslations()` hook-on keresztül

---

#### Story 0.9 – Self-hosted GitHub Actions Runner (Hetzner)
> **Branch:** `story/0.9-github-runner`  
> CI/CD self-hosted runner felállítása a Hetzner VPS-en

- [ ] `runner/docker-compose.runner.yml` létrehozása (`myoung34/github-runner` image)
- [ ] Docker socket mount konfigurálva (Testcontainers és image build miatt)
- [ ] `runner/env.example` fájl az összes szükséges változóval
- [ ] GitHub repo → Settings → Actions → Runners → New self-hosted runner → regisztráció
- [ ] Runner label-ek: `self-hosted, hetzner, linux`
- [ ] GitHub Secrets beállítása: `HETZNER_HOST`, `HETZNER_USER`, `HETZNER_SSH_KEY`, `PROD_ENV_FILE`
- [ ] Erőforrás limit: 1 CPU, 2 GB RAM (ne nyomja el a production stacket)

**Elfogadási kritériumok:**
- A runner megjelenik GitHub-on "Idle" státusszal
- Egy manuálisan indított teszt workflow sikeresen lefut a self-hosted runneren
- A runner leállása Grafana alertet küld (process monitor)
- `runner/.env` soha nem kerül verziókezelésbe

**Megszorítások:**
- A `RUNNER_TOKEN` 48 óráig érvényes – újraregisztrációhoz megújítandó
- MVP fázisban egyetlen runner elegendő (párhuzamos job-ok nem mennek egyidejűleg)

---

#### Story 0.10 – CI/CD Pipeline: PR checks + Deploy + Release
> **Branch:** `story/0.10-cicd-workflows`  
> GitHub Actions workflow-ok: PR ellenőrzés, automatikus deploy, release

- [ ] `.github/workflows/pr.yml` – Pull Request ellenőrzések:
  - `dotnet format --verify-no-changes` (kódformázás)
  - `dotnet build --configuration Release`
  - Unit tesztek: `dotnet test --filter Category=Unit`
  - Integrációs tesztek: `dotnet test --filter Category=Integration` (Testcontainers)
  - Coverage gate: Coverlet + ReportGenerator, ≥ 70% line coverage kötelező
  - PR coverage komment (automatikus, frissülő)
  - Frontend: `tsc --noEmit` + `eslint` + `next build`
  - CHANGELOG.md `[Unreleased]` szekció frissítve ellenőrzés
- [ ] `.github/workflows/deploy.yml` – Push to main:
  - Tesztek újrafuttatása (deploy nem megy tört teszteken)
  - Docker image build: `rezilio-api`, `rezilio-frontend`
  - Push ghcr.io-ra (`sha-<commit>` és `main` taggel)
  - SSH deploy Hetzner VPS-re: `docker compose pull + up -d`
  - Health check: `curl --fail https://app.rezilio.hu/health`
  - Automatikus rollback ha health check sikertelen
- [ ] `.github/workflows/release.yml` – Push tag `v*.*.*`:
  - `Directory.Build.props` verziószám vs. tag egyezés ellenőrzés
  - CHANGELOG.md tartalmaz-e bejegyzést az adott verzióhoz
  - Docker image re-tag: `sha-...` → `v1.0.0` + `latest`
  - GitHub Release létrehozás (CHANGELOG.md szekció automatikusan a release leírásba)
- [ ] Branch protection rule a `main` branch-en:
  - PR kötelező (közvetlen push tiltva)
  - `pr.yml` pipeline zöld → merge engedélyezett
  - Legalább 1 reviewer jóváhagyás (opcionális, ha egyszemélyes fejlesztés)
- [ ] `Testcontainers.PostgreSql` NuGet csomag hozzáadva (tesztprojektekhez)
- [ ] xUnit `[Trait("Category", "Unit")]` és `[Trait("Category", "Integration")]` konvenciók alkalmazva
- [ ] `Directory.Build.props`: `<Version>0.1.0</Version>` beállítva

**Elfogadási kritériumok:**
- PR nyitásakor automatikusan elindul a `pr.yml` pipeline
- Coverage < 70% esetén a pipeline piros és a PR nem mergelődhet
- Push `main`-re → 10 percen belül az új image él production-ban (`/health` visszaigazolja)
- Tag `v0.1.0` push → GitHub Release létrejön a CHANGELOG.md tartalommal
- Rollback: szimulált hibás deploy esetén a health check detektál és a pipeline fail-el

**Megszorítások:**
- ❌ Nincs staging környezet – minden deploy közvetlenül production
- A deploy workflow csak akkor fut, ha a tesztek zöldek
- `PROD_ENV_FILE` secret rotálása szükséges, ha a production `.env` változik

---

## EPIC ORG – Organization modul (Master data)

> **Cél:** Importálható referencia/alap adatok – szervezeti egységek, helyszínek, ügyfelek, beszállítók, kulcsszemélyek, IT rendszerek, üzleti folyamatok. Mindig aktív (alap) modul.  
> **Időtartam:** ~3 hét  
> **Előfeltétel:** EPIC 0 teljes

---

### EPIC-ORG / Sprint 2b – Organization infrastruktúra + import engine

#### Story ORG.1 – Organization modul struktúra és közös domain
> **Branch:** `story/org.1-organization-module-base`  
> `Modules/Organization/` felállítása, közös alaptípusok

- [ ] `Modules/Organization/` mappa struktúra létrehozása (Domain, Application, Infrastructure)
- [ ] `OrganizationModule.cs` – modul regisztráció (`IWolverineExtension`)
- [ ] `OrganizationDbContext` EF Core kontextus
- [ ] `ImportJob` aggregate (Id, TenantId, EntityType, Status, TotalRows, SuccessRows, ErrorRows, Results)
- [ ] `EntityType` enum: `OrganizationalUnit`, `Location`, `Customer`, `Supplier`, `KeyPerson`, `ItSystem`, `BusinessProcess`
- [ ] `ImportJobCreated`, `ImportJobCompleted`, `ImportJobFailed` domain event-ek
- [ ] `ImportJobsDbContext` + migráció

**Elfogadási kritériumok:**
- Az `OrganizationModule` regisztrálva van a Wolverine pipeline-ban, felismeri a modul Command-jait
- `ImportJob` aggregate EF Core migrációval létrejön az adatbázisban
- Az `ImportJob` státuszgép helyesen működik: `Pending → Validating → Valid/Invalid → Importing → Completed/Failed`

**Megszorítások:**
- Az `ImportJob` minden tenant adatát TenantId-vel izoláltan tárolja
- Az `EntityType` enum bővítése visszafelé kompatibilis (meglévő adatok nem érintődnek)

---

#### Story ORG.2 – Excel import infrastruktúra (ClosedXML)
> **Branch:** `story/org.2-excel-import-engine`  
> Általános Excel template generálás és import parse engine

- [ ] ClosedXML NuGet csomag hozzáadása (MIT licenc)
- [ ] `IExcelTemplateGenerator` interface + implementáció
- [ ] `IExcelImportParser<T>` interface + általános implementáció
- [ ] Excel template generátor minden EntityType-hoz (fejléc, kötelező mezők, validáció, példasor)
- [ ] Validációs pipeline: sor-szintű hibák összegyűjtése (nem dob exception-t)
- [ ] `DownloadImportTemplate` Query + Handler (EntityType alapján)
- [ ] `UploadAndValidateImport` Command + Handler → ImportJob létrehozása + validálás
- [ ] `ConfirmImport` Command + Handler → tényleges adatbetöltés
- [ ] `GetImportJobStatus` Query + Handler
- [ ] `GetImportJobResults` Query + Handler (hibás sorok részletezése)

**Elfogadási kritériumok:**
- Template letöltés: az Excel fájl tartalmaz fejlécsort, példasort, és kötelező mező jelölést
- Validáció: hibás sorok pontos sor- és mezőhivatkozással visszajelzést adnak (pl. „3. sor, Ország: érvénytelen ISO kód")
- Hibás sorokkal feltöltött fájl esetén `ConfirmImport` nem hajtható végre (csak ha `ImportJob.Status == Valid`)
- 500 soros importfájl teljesítménye: validáció < 3 másodperc
- Unit teszt: validáció lefedi a kötelező mező hiányt, érvénytelen formátumot, duplikált kódot

**Megszorítások:**
- ❌ Csak `.xlsx` formátum támogatott – `.csv`, `.xls` nem
- ❌ Az import nem ír vissza a forrásrendszerbe (read-only import)
- A sablonok letölthetők és újra feltölthetők – az adatbetöltés idempotens (ha a kód már létezik, frissít, nem duplikál)

---

#### Story ORG.3 – OrganizationalUnit (SZMSZ / szervezeti hierarchia)
> **Branch:** `story/org.3-organizational-units`

- [ ] `OrganizationalUnit` aggregate (Id, TenantId, Code, Name, ParentUnitId?, Level, Type, ManagerName?, EmployeeCount?, IsActive)
- [ ] EF Core migráció
- [ ] Excel import parser és template
- [ ] `CreateOrganizationalUnit` / `UpdateOrganizationalUnit` / `DeactivateOrganizationalUnit` Command + Handler + Validator
- [ ] `GetOrganizationalUnitById` / `GetOrganizationalUnits` Query (fa struktúra)

**Elfogadási kritériumok:**
- Hierarchikus fa helyesen épül fel (parent-child kapcsolat, ciklus detektálás)
- Szülő deaktiválásakor a gyermek egységek is deaktiválódnak (vagy warning kiadódik)
- Az Excel import helyesen tölt be minimum 3 szintű szervezeti hierarchiát
- `Code` mező egyedi tenant szinten (duplikált kód import hibát ad)

---

#### Story ORG.4 – Location (Telephelyek)
> **Branch:** `story/org.4-locations`

- [ ] `Location` aggregate (Id, TenantId, Code, Name, Address, City, Country, Region?, Type, IsHeadquarters, IsActive)
- [ ] EF Core migráció
- [ ] Excel import parser és template
- [ ] `CreateLocation` / `UpdateLocation` / `DeactivateLocation` Command + Handler + Validator
- [ ] `GetLocationById` / `GetLocations` Query

**Elfogadási kritériumok:**
- Csak egy telephely lehet `IsHeadquarters = true` egy tenanten belül (üzleti szabály)
- `Country` mező ISO 3166-1 alpha-2 kód validálva
- Import: duplikált `Code` frissítést végez, nem létrehozást

---

#### Story ORG.5 – Customer (Ügyfelek)
> **Branch:** `story/org.5-customers`

- [ ] `Customer` aggregate (Id, TenantId, Code, Name, Industry, Country, Tier?, AnnualRevenue? [Money], IsActive)
- [ ] EF Core migráció
- [ ] Excel import parser és template
- [ ] `CreateCustomer` / `UpdateCustomer` / `DeactivateCustomer` Command + Handler + Validator
- [ ] `GetCustomerById` / `GetCustomers` Query

**Elfogadási kritériumok:**
- `AnnualRevenue` `Money` value object-ként tárolódik (Amount + CurrencyCode)
- Excel import: a pénznem oszlop érvényes ISO 4217 kódot vár, különben sor-szintű hiba
- `Code` egyedi tenant szinten

---

#### Story ORG.6 – Supplier (Beszállítók)
> **Branch:** `story/org.6-suppliers`

- [ ] `Supplier` aggregate (Id, TenantId, Code, Name, Category, Country, CriticalityLevel, ContactName?, ContactEmail?, ContractExpiry?, IsActive)
- [ ] EF Core migráció
- [ ] Excel import parser és template
- [ ] `CreateSupplier` / `UpdateSupplier` / `DeactivateSupplier` Command + Handler + Validator
- [ ] `GetSupplierById` / `GetSuppliers` Query

**Elfogadási kritériumok:**
- `CriticalityLevel` enum értéke validált (Low, Medium, High, Critical)
- `ContractExpiry` jövőbeli dátum (múlt dátum figyelmeztetést, nem hibát dob)
- `ContactEmail` érvényes email formátum

---

#### Story ORG.7 – KeyPerson (Kulcsszemélyek)
> **Branch:** `story/org.7-key-persons`

- [ ] `KeyPerson` aggregate (Id, TenantId, Name, Title, Department, OrgUnitId?, Email?, Phone?, BackupPersonName?, IsActive)
- [ ] EF Core migráció
- [ ] Excel import parser és template
- [ ] `CreateKeyPerson` / `UpdateKeyPerson` / `DeactivateKeyPerson` Command + Handler + Validator
- [ ] `GetKeyPersonById` / `GetKeyPersons` Query

**Elfogadási kritériumok:**
- `OrgUnitId` ha meg van adva, létező szervezeti egységre mutat (FK validáció)
- `Email` ha meg van adva, egyedi tenant szinten (figyelmeztetés, nem hiba import esetén)
- A kulcsszemélyhez rendelt `BackupPersonName` megjelenik a részletező nézetben

---

#### Story ORG.8 – ItSystem (IT rendszerek)
> **Branch:** `story/org.8-it-systems`

- [ ] `ItSystem` aggregate (Id, TenantId, Code, Name, Type, Vendor?, Version?, HostingType, OwnerId?, SupportedOrgUnitIds[], CriticalityLevel, IsActive)
- [ ] EF Core migráció (JSONB tömb a `SupportedOrgUnitIds`-hoz)
- [ ] Excel import parser és template
- [ ] `CreateItSystem` / `UpdateItSystem` / `DeactivateItSystem` Command + Handler + Validator
- [ ] `GetItSystemById` / `GetItSystems` Query

**Elfogadási kritériumok:**
- `HostingType` enum validált (OnPrem, Cloud, Hybrid)
- `SupportedOrgUnitIds` JSONB-ben tárolódik, lekérdezhetők az adott szervezeti egységhez tartozó rendszerek
- Import: `OwnerId` opcionális, ha megadva, létező `KeyPerson` kóddal matchel

---

#### Story ORG.9 – BusinessProcess (Üzleti folyamatok)
> **Branch:** `story/org.9-business-processes`

- [ ] `BusinessProcess` aggregate (Id, TenantId, Code, Name, OwnerId?, OrgUnitId?, Category, CriticalityLevel, MaxTolerableDowntime?, RecoveryTimeObjective?, DependsOnSystemIds[], IsActive)
- [ ] EF Core migráció (JSONB tömb a `DependsOnSystemIds`-hoz)
- [ ] Excel import parser és template
- [ ] `CreateBusinessProcess` / `UpdateBusinessProcess` / `DeactivateBusinessProcess` Command + Handler + Validator
- [ ] `GetBusinessProcessById` / `GetBusinessProcesses` Query

**Elfogadási kritériumok:**
- `RecoveryTimeObjective` ≤ `MaxTolerableDowntime` üzleti szabály (RTO nem lehet nagyobb az MTD-nél)
- `DependsOnSystemIds` JSONB-ben tárolódik, körkörös függőség nem megengedett
- Import: `CriticalityLevel` kötelező mező, érvényes értékkészletből

---

### EPIC-ORG / Sprint 2c – Organization Frontend

#### Story ORG.10 – Organization modul UI
> **Branch:** `story/org.10-organization-ui`  
> Listázó, részletező és import oldalak minden entity típushoz

- [ ] Organization navigáció a sidebarban (Szervezet, Helyszínek, Ügyfelek, Beszállítók, Kulcsszemélyek, IT Rendszerek, Folyamatok)
- [ ] Generikus lista komponens (keresés, lapozás, szűrők, aktív/inaktív toggle)
- [ ] Generikus részletező oldal
- [ ] Import flow komponens (újrahasználható minden entity típushoz):
  - Template letöltés gomb
  - Fájl feltöltés (drag & drop + fájlválasztó)
  - Validációs eredmény preview tábla (hibás sorok piros, helyes sorok zöld)
  - Megerősítő gomb + eredmény összefoglaló
- [ ] OrganizationalUnit hierarchia fa nézet
- [ ] `messages/en.json` és `messages/hu.json` bővítése (Organization szekció)

**Elfogadási kritériumok:**
- Az import flow minden lépése működik end-to-end (template le → feltöltés → preview → megerősítés)
- Hibás import esetén a felhasználó sor- és mezőszintű hibaüzenetet lát
- Csak sikeres validáció után aktív a "Megerősítés" gomb
- Az OrganizationalUnit fa megjelenítés indentált, összecsuktható
- Minden szöveg `useTranslations()` hook-on keresztül, magyar és angol fordítással

**Megszorítások:**
- Maximálisan 5000 sorós import fájl fogadható el a UI-on (felette figyelmeztetés)
- A lista oldalon maximum 100 elem jelenik meg lapozás nélkül

---

## EPIC 1 – MVP (Core modulok)

> **Cél:** Önmagában értékesíthető Basic csomag. Teljes kockázati életciklus.  
> **Időtartam:** ~8 hét (4 sprint)  
> **Előfeltétel:** EPIC 0 + EPIC ORG teljes

---

### EPIC-1 / Sprint 3 – RiskRegister API

#### Story 1.1 – Risk aggregate és domain
> **Branch:** `story/1.1-risk-aggregate`

- [ ] `Risk` aggregate (Id, TenantId, DomainId, Title, Description, Category, OwnerId, Status)
- [ ] `RiskStatus` enum (Draft, Active, UnderReview, Treated, Closed, Archived)
- [ ] `RiskCategory` value object
- [ ] `RiskDomain` entitás (IT, Financial, ESG stb.)
- [ ] `Migrations/` mappa létrehozása
- [ ] `RisksDbContext` + migráció

**Elfogadási kritériumok:**
- `Risk` aggregate invariánsai védve: nem archivált kockázat nem zárható le közvetlenül (státuszgép)
- `OwnerId` egy meglévő `KeyPerson`-ra mutat (validálva a Handler-ben)
- EF Core migráció hibamentesen lefut friss adatbázison

---

#### Story 1.2 – RiskRegister Command slice-ok
> **Branch:** `story/1.2-risk-commands`

- [ ] `CreateRisk` Command + Handler + Validator → `RiskCreated` event
- [ ] `UpdateRisk` Command + Handler + Validator → `RiskUpdated` event
- [ ] `ArchiveRisk` Command + Handler → `RiskArchived` event
- [ ] `AssignRiskOwner` Command + Handler → `RiskOwnerAssigned` event
- [ ] `DeleteRisk` Command + Handler (soft delete)

**Elfogadási kritériumok:**
- Minden Command Validator-ral rendelkezik, kötelező mezők hiánya `400 Bad Request`-et eredményez
- `RiskCreated` domain event tüzel a `CreateRisk` Handler lefutása után
- Törölt kockázat nem módosítható (soft delete-et a Handler ellenőrzi)
- Integration teszt: teljes Command → Handler → adatbázis → Event flow lefedve

---

#### Story 1.3 – RiskRegister Query slice-ok
> **Branch:** `story/1.3-risk-queries`

- [ ] `GetRiskById` Query + Handler
- [ ] `GetRisksByDomain` Query + Handler (szűrés, lapozás)
- [ ] `GetRisksByOwner` Query + Handler
- [ ] `SearchRisks` Query + Handler (full-text keresés)

**Elfogadási kritériumok:**
- Más tenant kockázatai nem jelennek meg (TenantId szűrés minden query-ben kötelező)
- `GetRisksByDomain` lapozása helyes (skip/take, total count visszaadva)
- `SearchRisks` minimum title és description mezőkben keres

---

### EPIC-1 / Sprint 4 – RiskRegister UI

#### Story 1.4 – Kockázat lista és kezelés
> **Branch:** `story/1.4-risk-list-ui`

- [ ] Kockázat lista oldal (táblázat, szűrés, keresés, lapozás)
- [ ] Kockázat részletező oldal
- [ ] Kockázat létrehozás / szerkesztés form
- [ ] Kockázat archiválás / törlés
- [ ] RiskDomain selector (IT, Financial stb.)
- [ ] Risk owner hozzárendelés UI (KeyPerson listából)

**Elfogadási kritériumok:**
- Kockázat létrehozható, szerkeszthető, archiválható a UI-on keresztül
- A lista szűrhető domain és státusz szerint, kereshető szövegre
- Nem engedélyezett műveletek (pl. Viewer role archiválás) gombjai inaktívak vagy rejtettek

---

#### Story 1.5 – Alap dashboard
> **Branch:** `story/1.5-basic-dashboard`

- [ ] Kockázatok státusz szerinti összesítése (számok, badge-ek)
- [ ] Legutóbb módosított kockázatok lista
- [ ] Navigációs sidebar modulokkal (aktív/inaktív állapottal)

**Elfogadási kritériumok:**
- Dashboard adatai valós idejű API hívásokból jönnek (nem hardcoded)
- Inaktív modul a sidebarban szürkén jelenik meg, kattintásra "Upgrade szükséges" komponenst mutat

---

### EPIC-1 / Sprint 5 – Assessment + Treatment API

#### Story 1.6 – Assessment modul
> **Branch:** `story/1.6-assessment-module`

- [ ] `Assessment` aggregate (RiskId, LikelihoodScore, ImpactScore, RiskScore, Type)
- [ ] `CalculateRiskScore` domain service (likelihood × impact)
- [ ] `CreateAssessment` / `SubmitAssessment` / `ApproveAssessment` Command + Handler
- [ ] `GetAssessmentById` / `GetRiskHeatMap` / `GetAssessmentHistory` Query

**Elfogadási kritériumok:**
- `RiskScore = LikelihoodScore × ImpactScore` (1–5 skálán, eredmény 1–25)
- `GetRiskHeatMap` visszaadja az 5×5 mátrix minden cellájához tartozó kockázatok listáját
- Egy kockázathoz több assessment is lehet (előzmény), de csak egy aktív
- Jóváhagyás (`ApproveAssessment`) csak `RiskManager` vagy `Admin` role-lal lehetséges

---

#### Story 1.7 – Treatment modul
> **Branch:** `story/1.7-treatment-module`

- [ ] `TreatmentPlan` aggregate + `Control` entity + `Action` entity
- [ ] `TreatmentStrategy` enum (Accept, Avoid, Reduce, Transfer)
- [ ] `CreateTreatmentPlan` / `AddControl` / `UpdateControlStatus` / `AssignAction` / `CompleteAction` Command + Handler
- [ ] `GetTreatmentByRisk` / `GetOverdueActions` Query

**Elfogadási kritériumok:**
- Egy kockázathoz csak egy aktív `TreatmentPlan` lehet
- Lejárt határidejű action-ök megjelennek a `GetOverdueActions` query-ben
- `CompleteAction` után a kontroll státusza frissítve van

---

### EPIC-1 / Sprint 6 – Assessment + Treatment UI

#### Story 1.8 – Assessment UI
> **Branch:** `story/1.8-assessment-ui`

- [ ] Értékelési form (likelihood × impact slider/selector)
- [ ] Kockázati hőtérkép vizualizáció (5×5, Recharts/Nivo)
- [ ] Inherens vs. reziduális kockázat megjelenítés
- [ ] Értékelési előzmények timeline

**Elfogadási kritériumok:**
- A hőtérkép vizuálisan helyes (piros = magas kockázat, zöld = alacsony)
- Kockázatra kattintva a térkép celláján a részletező oldal nyílik meg
- Inherens és reziduális értékelés egymás mellett megjelenítve

---

#### Story 1.9 – Treatment UI
> **Branch:** `story/1.9-treatment-ui`

- [ ] Kezelési terv oldal (stratégia, kontrollok, akciók)
- [ ] Kontrollok listája + státusz kezelés
- [ ] Akciólista, felelős, határidő megjelenítés
- [ ] Lejárt akciók kiemelése

**Elfogadási kritériumok:**
- Lejárt akciók piros kiemelést kapnak
- Kontroll státusz módosítható drag-and-drop vagy gombokkal
- Akció létrehozásakor kötelező a felelős személy és a határidő

---

## EPIC 2 – Prémium I. (Monitoring + Incidents)

> **Cél:** Professional csomag. KRI figyelés, incidenskezelés.  
> **Időtartam:** ~8 hét (4 sprint)  
> **Előfeltétel:** EPIC 1 teljes

---

### EPIC-2 / Sprint 7 – Monitoring API
> **Branch:** `story/2.7-monitoring-api`

- [ ] `KRI` aggregate (Name, RiskId, WarningThreshold, CriticalThreshold, Unit)
- [ ] `CreateKRI` / `RecordKRIValue` / `SetKRIThreshold` Command-ok
- [ ] `GetKRIStatus` / `GetKRITrend` / `GetUpcomingReviews` Query-k
- [ ] Wolverine scheduled message: küszöbérték-ellenőrzés (napi)
- [ ] `KRIThresholdBreached` domain event

**Elfogadási kritériumok:**
- Küszöbérték átlépésekor `KRIThresholdBreached` event tüzel és naplózódik
- A napi ütemezett ellenőrzés Wolverine scheduled message-ként fut
- `GetKRITrend` az utolsó N mérési értéket időrendben adja vissza

---

### EPIC-2 / Sprint 8 – Monitoring UI
> **Branch:** `story/2.8-monitoring-ui`

- [ ] KRI dashboard, trend grafikon, küszöbérték beállítás UI, felülvizsgálati naptár

**Elfogadási kritériumok:**
- Küszöbérték-sértés vizuálisan kiemelve a dashboardon (piros badge)
- Trend grafikon legalább 30 napos adatot jelenít meg

---

### EPIC-2 / Sprint 9 – Incidents API
> **Branch:** `story/2.9-incidents-api`

- [ ] `Incident` aggregate, `ReportIncident` / `InvestigateIncident` / `CloseIncident` Command-ok
- [ ] `LinkIncidentToRisk` Command, `GetIncidentById` / `GetIncidentsByRisk` Query-k
- [ ] `IncidentReported` domain event

**Elfogadási kritériumok:**
- Incidens kockázathoz köthető, de kockázat nélkül is rögzíthető
- Lezárt incidenshez nem rögzíthető új adat

---

### EPIC-2 / Sprint 10 – Incidents UI + Licensing UI
> **Branch:** `story/2.10-incidents-licensing-ui`

- [ ] Incidensbejelentő form, incidens–kockázat kapcsolat vizualizáció
- [ ] Licensing admin oldal, trial indítás UI, "Upgrade szükséges" flow

**Elfogadási kritériumok:**
- Incidens–kockázat kapcsolat grafikusan megjelenítve
- Admin csak `Admin` role-lal látja a licensing oldalt

---

## EPIC 3 – Prémium II. (Compliance + Reporting)

> **Cél:** Enterprise csomag első fele.  
> **Időtartam:** ~8 hét (4 sprint)  
> **Előfeltétel:** EPIC 2 teljes

---

### EPIC-3 / Sprint 11 – Compliance API
> **Branch:** `story/3.11-compliance-api`

- [ ] `ComplianceFramework` aggregate (ISO 31000, GDPR, NIS2, Basel)
- [ ] `AddComplianceFramework` / `MapControlToRequirement` / `UpdateComplianceStatus`
- [ ] `GetComplianceGaps` / `GetFrameworkCoverage` Query-k

**Elfogadási kritériumok:**
- Egy kontroll több compliance követelményhez is hozzárendelhető
- `GetComplianceGaps` azokat a követelményeket adja vissza, amelyekhez nincs mappelt kontroll

---

### EPIC-3 / Sprint 12 – Compliance UI
> **Branch:** `story/3.12-compliance-ui`

- [ ] Framework selector, követelmény–kontroll mapping UI, gap vizualizáció, státusz összefoglaló

**Elfogadási kritériumok:**
- A gap vizualizáció százalékos lefedettséget mutat keretrendszerenként
- Mapping drag-and-drop vagy többlépéses formmal elvégezhető

---

### EPIC-3 / Sprint 13 – Reporting API
> **Branch:** `story/3.13-reporting-api`

- [ ] `GenerateExecutiveSummary` / `GenerateRiskReport` Command (async, PDF)
- [ ] `ScheduleReport` Command + Wolverine scheduled delivery
- [ ] `GetReportById` / `GetScheduledReports` Query-k
- [ ] PDF generálás: QuestPDF (open source, MIT licenc)

**Elfogadási kritériumok:**
- PDF generálás async (nem blokkolja a kérést) – `202 Accepted` + poll végpont
- Ütemezett riport Wolverine scheduled message-ként fut és e-mailt küld
- A generált PDF letölthető az API-n keresztül

**Megszorítások:**
- ❌ Nincs iTextSharp (AGPL licenc) – QuestPDF vagy más MIT/Apache licencű könyvtár

---

### EPIC-3 / Sprint 14 – Reporting UI + ESG alap
> **Branch:** `story/3.14-reporting-esgesl-ui`

- [ ] Riport generáló oldal, ütemezett riportok kezelése, ESG modul alap struktúra

**Elfogadási kritériumok:**
- A riport generálás folyamata állapotjelzővel mutatja a haladást (generálás alatt → kész → letölthető)

---

## EPIC 4 – Enterprise + SaaS

> **Cél:** AI modul, B2B multitenancy, production-ready.  
> **Időtartam:** ~6 hét (3 sprint)  
> **Előfeltétel:** EPIC 3 teljes

---

### EPIC-4 / Sprint 15 – AIInsights modul
> **Branch:** `story/4.15-ai-insights`

- [ ] OpenAI / open-source LLM integráció (konfigurálható endpoint)
- [ ] Kockázati mintázat felismerés, automatikus javaslatok, KRI anomália detektálás

**Elfogadási kritériumok:**
- Az LLM endpoint konfigurálható (`appsettings.json`), nem hardcoded
- AI javaslat csak javaslatként jelenik meg – a felhasználó erősíti meg, automatikusan nem módosít adatot

**Megszorítások:**
- Az AI modul opcionális – nélküle az alkalmazás teljes funkcionalitással működik

---

### EPIC-4 / Sprint 16 – B2B Multitenancy (Phase 2)
> **Branch:** `story/4.16-multitenancy-phase2`

- [ ] TenantId szűrés teljes körű validálása minden entitáson
- [ ] Tenant regisztráció és onboarding flow
- [ ] Iparági sablonok (IT, Financial, ESG template betöltés)
- [ ] Tenant admin felület, számlázási integráció alap

**Elfogadási kritériumok:**
- Tenant A adatai semmilyen körülmények között nem jelennek meg Tenant B számára (izolációs teszt)
- Tenant onboarding: Keycloak `tenant_id` user attribute automatikusan beállítódik
- Integration teszt: két párhuzamos tenant teljes adatizoláció mellett működik

---

### EPIC-4 / Sprint 17 – Hardening + Observability + Deployment
> **Branch:** `story/4.17-observability-deployment`

- [ ] OpenTelemetry SDK konfiguráció (tracing, metrics, logging)
  - `AddAspNetCoreInstrumentation()`, `AddEntityFrameworkCoreInstrumentation()`, `AddWolverineInstrumentation()`
  - Dev: console exporter; Prod: OTel Collector → Prometheus + Loki
- [ ] OTel Collector konfiguráció (`otel/otel-collector-config.yaml`)
- [ ] Prometheus konfiguráció + Grafana dashboardok:
  - API health (kérések, hibák, latencia p50/p95/p99)
  - DB performance (slow query-k, connection pool)
  - Business metrikák (aktív tenantek, kockázat bejegyzések száma)
  - Infrastruktúra (CPU, RAM, disk)
- [ ] Loki + Promtail (Docker container log aggregáció)
- [ ] Grafana provisioning (`/grafana/provisioning/`) – dashboardok kódban
- [ ] Grafana Alerting: e-mail értesítés kritikus hibáknál (SMTP)
- [ ] `docker-compose.prod.yml` – teljes production stack (Traefik, API, frontend, Postgres, Keycloak, OTel stack)
- [ ] GitHub Actions CI/CD: Docker build + push → SSH deploy Hetzner VPS-re
- [ ] Hetzner Firewall szabályok (80, 443, 22)
- [ ] Teljesítmény optimalizálás, load testing
- [ ] Biztonsági audit (OWASP alapján)
- [ ] Audit napló UI

**Elfogadási kritériumok:**
- Grafana dashboardon megjelenik az API request rate és latencia valós forgalom alapján
- Loki-ban kereshetők a container logok Grafana Explore-ban
- `docker compose -f docker-compose.prod.yml up` elindítja a teljes production stacket
- GitHub Actions pipeline: kód merge → Docker image → Hetzner deploy < 10 perc
- Grafana alert tüzel tesztelhetően (szimulált hiba esetén e-mail érkezik)
- OWASP Top 10 ellenőrzés elvégezve, kritikus lelet nincs

**Megszorítások:**
- ❌ Nincs Azure Application Insights SDK
- ❌ Nincs Azure Container Apps – a deployment Hetzner VPS + Docker Compose
- Minden secret `.env` fájlban, soha nem a repóban

---

*Ez a dokumentum folyamatosan bővül. Új story-k és task-ok kerülnek ide minden tervezési session után.*
