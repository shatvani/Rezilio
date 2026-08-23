# CLAUDE.md – AI Kontextus fájl

> Ezt a fájlt a Claude (és más AI eszközök) olvassák minden session elején.  
> Tartalmazza az architektúrális döntéseket, konvenciókat és a "mit NE csinálj" szabályokat.

---

## Mit épít ez a projekt?

Vállalati kockázatelemző és -kezelő SaaS platform. Segít cégeknek azonosítani, értékelni, nyomon követni és kezelni üzleti kockázataikat – több kockázati területen (IT, pénzügyi, ESG, operacionális, megfelelőségi) egyidejűleg.

Részletes specifikáció: `docs/SPEC.md`

---

## Technológiai stack

### Backend
- **ASP.NET Core Minimal API** – HTTP réteg
- **Wolverine** – Message Bus, Command/Query dispatch, scheduled messages, middleware pipeline
- **EF Core 10** – adatbázis hozzáférés, per-modul DbContext
- **PostgreSQL 16** – egyetlen adatbázis provider (Npgsql), nincs pluggable absztrakció
- **Keycloak** – Identity Provider, self-hosted, OpenID Connect JWT validáció
- **FluentValidation** – input validáció

### Frontend
- **Next.js 14+** (App Router)
- **TanStack Query** – szerver állapot kezelés
- **next-intl** – i18n, dinamikusan bővíthető JSON nyelvi fájlok
- **Tailwind CSS** – utility-first CSS framework
- **shadcn/ui** – komponens könyvtár (copy-paste alapú, nem npm függőség)
- **Recharts / Nivo** – vizualizáció (kockázati hőtérkép, KRI trendek)
- **TypeScript** – kötelező minden fájlban

### Infrastruktúra
- **Docker Compose** – lokális dev és production
- **Hetzner VPS** – production hosting
- **Traefik** – reverse proxy, automatikus Let's Encrypt SSL
- **OpenTelemetry + Grafana + Prometheus + Loki** – observability (self-hosted)

---

## Architektúrális elvek

### Modular Monolith + VSA
- Minden üzleti képesség egy **modul** alatt él (`src/Modules/<ModuleName>/`)
- Minden modul **Vertical Slice Architecture** szerint épül fel
- Egy slice = egy üzleti művelet = Command vagy Query + Handler + (opcionális) Validator
- Modulok között **nincs közvetlen referencia** – csak Wolverine event-eken keresztül kommunikálnak

### DDD elvek
- Aggregátok védik az invariánsokat, csak public metódusaikon keresztül módosíthatók
- Value Object-ek immutable-ök, értékalapú egyenlőséggel
- Domain Event-eket az aggregátok publikálják, Wolverine dispatch-eli őket
- Application logika a Handler-ben van, nem a Controller-ben (nincs Controller – Minimal API endpoint-ok vannak)

### Multitenancy
- **Minden entitáson** kötelező a `TenantId` mező
- **Phase 1:** egyetlen fix `TenantId` érték, de az infrastruktúra már multitenancy-ready
- **Phase 2:** per-request TenantId resolution, teljes adatizoláció
- Az `ITenantContext` interface-t minden Handler megkapja DI-on keresztül

### Cross-cutting concerns

**Adatbázis (ADR-007)** – PostgreSQL 16, Npgsql:
- Egyetlen migráció mappa: `Migrations/`
- Nincs provider-switch logika, nincs `IDatabaseProvider` absztrakció
- ✅ PostgreSQL specifikus funkciók (JSONB, array típusok) bátran használhatók

**Auth / Identity (ADR-012)** – Keycloak OIDC:
- A REZILIO csak Keycloak JWT tokeneket validál
- Claims normalizálás: Keycloak claim-ek → `AppClaims` konstansok
- Enterprise federation (AD, Entra ID): Keycloak Identity Brokering kezeli, az alkalmazás kód nem változik
- ❌ Ne használj Keycloak-specifikus claim neveket a Handler-ekben – csak `AppClaims` konstansokat:
  - `AppClaims.UserId` (`app:user_id`)
  - `AppClaims.TenantId` (`app:tenant_id`)
  - `AppClaims.Email` (`app:email`)
  - `AppClaims.Roles` (`app:roles`)

**Pénznem (ADR-006)** – `Money` value object (`Amount` + `CurrencyCode`):
- Minden pénzügyi értéket `Money`-ként tárolj, ne sima `decimal`-ként
- Tenant alapértelmezett pénznem: `TenantSettings.DefaultCurrency`
- ❌ Ne égess be pénznem kódot (`"HUF"`) a kódba – mindig a tenant konfigból olvasd

**Lokalizáció (ADR-009)** – UI szövegek és adat fordítás:
- Backend hibaüzenetek: `IStringLocalizer<T>` – soha ne hardcode-olj szöveget
- Frontend: minden felhasználónak megjelenő szöveg `messages/<lang>.json`-ból jön
- Fallback: mindig `en` – az `en.json` mindig teljes kell legyen
- Új nyelv hozzáadása: új JSON fájl + `TenantSettings.SupportedLanguages` bővítése
- ❌ Ne írj UI szöveget hardcode-olva a komponensbe – `useTranslations()` hook kell

**Observability (ADR-013)** – OpenTelemetry + Grafana stack:
- Instrumentáció: `AddOpenTelemetry()` az ASP.NET Core DI-ban
- Dev: console exporter, prod: OTel Collector → Prometheus / Loki
- ❌ Ne használj Azure Application Insights SDK-t

---

## Kötelező konvenciók

### Projekt struktúra
```
src/
├── RiskAnalyzer.Api/                    # Minimal API host
├── RiskAnalyzer.SharedKernel/           # DDD base osztályok, közös value object-ek
└── Modules/
    └── <ModuleName>/
        ├── Domain/                      # Aggregátok, Value Object-ek, Domain Event-ek
        ├── Application/
        │   ├── Commands/
        │   │   └── <ActionName>/
        │   │       ├── <ActionName>Command.cs
        │   │       ├── <ActionName>Handler.cs
        │   │       └── <ActionName>Validator.cs
        │   └── Queries/
        │       └── <QueryName>/
        │           ├── <QueryName>Query.cs
        │           └── <QueryName>Handler.cs
        ├── Infrastructure/              # DbContext, egyéb infrastruktúra
        └── <ModuleName>Module.cs        # Modul regisztráció (IWolverineExtension)
```

### Slice naming
- Command-ok: `<Ige><Főnév>Command` – pl. `CreateRiskCommand`, `AssignRiskOwnerCommand`
- Query-k: `Get<Mit><Szűrő>Query` – pl. `GetRiskByIdQuery`, `GetRisksByDomainQuery`
- Handler-ek: ugyanaz a neve, `Handler` suffix-szel
- Domain Event-ek: `<Főnév><IgeMúlt>` – pl. `RiskCreated`, `AssessmentCompleted`

### Hibakezelés
- Handler-ek `Result<T>` típust adnak vissza (nem dobnak exception-t üzleti hibánál)
- Infrastruktúrális hibák mehetnek exception-ként (Wolverine kezeli)
- Minimal API endpoint-ok a `Result<T>`-t HTTP response-sá alakítják

### Licensz-ellenőrzés
- Minden prémium modul endpoint-ján kötelező a `RequireModule(ModuleType.X)` hívás
- A `ModuleAccessBehavior` Wolverine middleware automatikusan ellenőriz minden Command-ot és Query-t
- Deaktivált modulra érkező kérés: `403 Forbidden` + `ModuleNotLicensedException`

### Branch névképzési stratégia

Részletes döntés: `docs/ADR-014-versioning.md`

| Prefix | Mikor | Példa |
|---|---|---|
| `story/` | Új feature / story fejlesztése | `story/0.1-solution-structure` |
| `fix/` | Bug javítás (nem production) | `fix/42-import-row-validation` |
| `hotfix/` | Production hiba – azonnali javítás | `hotfix/1.0.1-keycloak-token-expiry` |
| `release/` | Release branch (changelog, verziószám) | `release/1.0.0` |
| `chore/` | Függőségek, konfig, dokumentáció, CI | `chore/update-closedxml` |
| `spike/` | Technikai proof-of-concept | `spike/otel-sampling-strategy` |

**Szabályok:**
- Kisbetűs, kötőjel (`-`) elválasztóval – soha nem szóköz, underscore, vagy extra slash
- Story branch neve tartalmazza a story ID-t: `story/0.5-keycloak-oidc`
- Maximális hossz: 60 karakter
- Merge kizárólag PR-on keresztül, branch törlése merge után
- `main` branch mindig production-ready állapotban van

### Verziókezelési stratégia

**Semantic Versioning 2.0:** `MAJOR.MINOR.PATCH`

| Szegmens | Mikor nő | Példa |
|---|---|---|
| MAJOR | Visszafelé nem kompatibilis API/adatmodell változás | `1.0.0 → 2.0.0` |
| MINOR | Új feature / modul (visszafelé kompatibilis) | `1.0.0 → 1.1.0` |
| PATCH | Bug fix, security patch, kisebb javítás | `1.0.0 → 1.0.1` |

- Fejlesztés alatt: `0.x.y` – nincs stabil API garancia
- Első stabil release: `1.0.0` (EPIC 1 lezárultával)
- Git tag formátum: `v1.0.0` (v prefix + SemVer)
- Docker image tag: `rezilio-api:1.0.0` és `rezilio-api:latest`
- Verzió helye: `Directory.Build.props` → `<Version>0.1.0</Version>`
- CHANGELOG.md: [Keep a Changelog](https://keepachangelog.com) formátum
- ✅ Minden PR-hoz kötelező a `CHANGELOG.md` `[Unreleased]` szekció frissítése

---

## Mit NE csinálj

### Architektúra
- ❌ **Ne generálj Repository pattern-t** EF Core DbContext fölé – a Handler-ek közvetlenül használják a DbContext-et
- ❌ **Ne javasolj microservice felbontást** – ez tudatos döntés, modular monolith marad
- ❌ **Ne hívj más modult közvetlenül** – csak Wolverine event-eken keresztül (kivétel: `Licensing.CheckModuleAccess`)
- ❌ **Ne tegyél üzleti logikát az API endpoint-ba** – az a Handler feladata
- ❌ **Ne felejtsd el a TenantId-t** egyetlen új entitáson sem

### Adatbázis
- ❌ **Ne adj hozzá pluggable DB provider absztrakciót** – az adatbázis PostgreSQL, kész
- ❌ **Ne hozz létre provider-specifikus migráció mappákat** (pl. `Migrations/SqlServer/`) – csak `Migrations/` létezik
- ❌ **Ne importálj MS SQL vagy Oracle specifikus NuGet csomagot**

### Auth
- ❌ **Ne implementálj LocalSql, AD vagy AzureEntraID auth providert** – Keycloak kezeli ezeket
- ❌ **Ne használj Keycloak-specifikus claim neveket** a Handler-ekben – csak `AppClaims` konstansokat
- ❌ **Ne tárolj felhasználói jelszót** a REZILIO adatbázisban – Keycloak feladata

### Observability
- ❌ **Ne használj Azure Application Insights SDK-t** – OpenTelemetry + Grafana stack van

### Kód
- ❌ **Ne használj `var` helyett típust** ahol az nem egyértelmű
- ❌ **Ne generálj `async void`** metódust
- ❌ **Ne használj magic string-eket** – enum vagy konstans
- ❌ **Ne generálj `.Result` vagy `.Wait()` hívást** async kódban

### Frontend
- ❌ **Ne használj `<form>` HTML elemet** React komponensben – event handler-ek kellenek
- ❌ **Ne tárolj szenzitív adatot localStorage-ban** – JWT-t httpOnly cookie-ban vagy memory-ban
- ❌ **Ne hívj API-t közvetlenül fetch-hel** – a `lib/api-client.ts` klienst használd

---

## CI/CD Pipeline (ADR-015)

### Workflow-ok (`.github/workflows/`)

| Fájl | Trigger | Mit csinál |
|---|---|---|
| `pr.yml` | PR → `main` | Build, unit + integrációs tesztek, coverage gate, frontend lint, CHANGELOG check |
| `deploy.yml` | Push → `main` | Tesztek → Docker build → ghcr.io push → Hetzner SSH deploy |
| `release.yml` | Push tag `v*` | Docker re-tag → GitHub Release (CHANGELOG szekció alapján) |

### Test kategóriák (xUnit)

```csharp
[Trait("Category", "Unit")]        // Gyors, nincs külső függőség
[Trait("Category", "Integration")] // Testcontainers PostgreSQL – self-hosted runneren fut
```

- **Coverage gate:** ≥ 70% line coverage kötelező, PR nem mergelődik alatta
- **CHANGELOG ellenőrzés:** minden PR-on kötelező a `[Unreleased]` szekció frissítése

### Self-hosted runner

- `runner/docker-compose.runner.yml` – Hetzner VPS-en futó Docker konténer
- Label: `self-hosted, hetzner, linux`
- Docker socket mount-olva → Testcontainers és `docker build` natívan működik
- Erőforrás limit: 1 CPU, 2 GB RAM (nem nyomja el a production stacket)

### GitHub Secrets (kötelező beállítani)

- `HETZNER_HOST`, `HETZNER_USER`, `HETZNER_SSH_KEY` – deployment SSH
- `PROD_ENV_FILE` – base64 kódolt `.env` fájl (deploy során kerül a VPS-re)
- `RUNNER_TOKEN` – runner regisztráció (48 óráig érvényes, runner compose-ban)

### Konvenciók

- ❌ **Ne commitolj a `main` branch-re direktben** – minden változás PR-on keresztül
- ❌ **Ne taggelj verziót ha a `Directory.Build.props` verziója nem egyezik** – a release pipeline ellenőrzi
- ✅ Release folyamat: `release/<version>` branch → changelog + verziószám frissítés → PR → `main` → tag → GitHub Release

---

## Aktuális fázis és fókusz

**Jelenlegi fázis:** Tervezés – dokumentáció elkészítése  
**Következő lépés:** Phase 0 / Sprint 1 – Backend infrastruktúra, Keycloak integráció

Részletes feladatlista: `docs/TASKS.md`

---

## Architektúrális döntések

Minden fontosabb döntés ADR formátumban dokumentálva:

| ADR | Döntés |
|---|---|
| [ADR-001](ADR-001-modular-monolith.md) | Modular Monolith, nem Microservice |
| [ADR-002](ADR-002-wolverine.md) | Wolverine mint Message Bus és HTTP framework |
| [ADR-003](ADR-003-feature-flag-licensing.md) | Egy image, feature flag alapú modul-aktiváció |
| [ADR-004](ADR-004-two-phase-multitenancy.md) | Kétfázisú multitenancy stratégia |
| [ADR-005](ADR-005-api-first.md) | API-first fejlesztés, UI 1-2 sprinttel lemarad |
| [ADR-006](ADR-006-money-currency.md) | Money value object, tenant szintű pénznem |
| [ADR-007](ADR-007-pluggable-database.md) | PostgreSQL fix választás – pluggable provider elvetve |
| [ADR-008](ADR-008-pluggable-auth.md) | Pluggable auth – felváltotta ADR-012 (Keycloak) |
| [ADR-009](ADR-009-i18n.md) | Többnyelvűség, next-intl, dinamikus nyelv hozzáadás |
| [ADR-011](ADR-011-infrastructure-deployment.md) | Hetzner VPS + Docker + Traefik + Let's Encrypt |
| [ADR-012](ADR-012-keycloak-idp.md) | Keycloak mint központi IdP, felváltja ADR-008-at |
| [ADR-013](ADR-013-observability.md) | Grafana + Prometheus + Loki observability stack |
| [ADR-014](ADR-014-versioning.md) | SemVer verziókezelés + Keep a Changelog |
| [ADR-015](ADR-015-cicd-pipeline.md) | CI/CD: GitHub Actions + Hetzner self-hosted runner + Testcontainers |
