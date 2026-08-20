# Risk Analyzer – Rendszerspecifikáció (SPEC.md)

> **Utolsó frissítés:** 2026-08-19 (v3 – Hetzner infra, Keycloak, Grafana stack, PostgreSQL fix)  
> **Státusz:** Tervezési fázis – folyamatosan bővül

---

## 1. Projekt áttekintés

### 1.1 Mit épít a rendszer?
Vállalati kockázatelemző és -kezelő SaaS platform, amely segít cégeknek azonosítani, értékelni, nyomon követni és kezelni az üzleti kockázataikat. A platform egyetlen terméken belül több kockázati területet (IT, pénzügyi, ESG, operacionális, megfelelőségi stb.) képes kiszolgálni, és idővel több független vállalatot (B2B SaaS multitenancy) is támogat.

### 1.2 Célpiac
- **Elsődleges:** Nagyvállalatok, amelyek belső kockázatkezelési eszközt keresnek több szervezeti egység számára
- **Másodlagos:** Közép- és nagyvállalatok SaaS előfizetéssel (Phase 2)

### 1.3 Üzleti értékajánlat
- Egyetlen platform, amely lefedi a teljes kockázati életciklust
- Több kockázati terület (domain) kezelése egy cégszintű nézőpontból
- Konfigurálható keretrendszer iparáganként (ISO 31000, NIST, GDPR, Basel stb.)
- Moduláris árazás: alap + prémium modulok
- Gyors onboarding iparági sablonokkal

---

## 2. Architektúra

### 2.1 Technológiai stack

| Réteg | Technológia |
|---|---|
| Frontend | Next.js 14+ (App Router), TanStack Query, Recharts/Nivo |
| Frontend i18n | next-intl (dinamikusan bővíthető nyelvi fájlok) |
| Backend API | ASP.NET Core Minimal API |
| Üzleti logika / Message Bus | Wolverine |
| ORM | EF Core 10, per-modul DbContext |
| Adatbázis | PostgreSQL 16 (Npgsql) |
| Auth / IdP | Keycloak (self-hosted, OpenID Connect) |
| Reverse proxy + SSL | Traefik + Let's Encrypt (automatikus megújítás) |
| Containerizáció | Docker Compose (dev + prod) |
| Hosting | Hetzner VPS (prod), lokális Docker (dev) |
| Observability | OpenTelemetry, Grafana + Prometheus + Loki |

### 2.2 Backend architektúra
- **Modular Monolith + Vertical Slice Architecture (VSA)**
- **Domain-Driven Design (DDD)** elvek alapján
- Minden modul önálló mappa, saját DbContext, saját domain, saját slice-ok
- Modulok közötti kommunikáció **kizárólag Wolverine domain event-eken** keresztül (kivétel: licensz-ellenőrzés, ami szinkron middleware)
- Egyetlen Docker image tartalmaz minden modult

### 2.3 Multitenancy stratégia (kétfázisú)

**Phase 1 – Single Tenant + Multi Risk Domain**
- Egyetlen fix TenantId az adatbázisban
- Egy cégen belül több kockázati terület (RiskDomain) kezelése
- Felhasználók szervezeti egységenként kapnak jogosultságot

**Phase 2 – B2B SaaS Multitenancy**
- Minden entitáson TenantId szűrés (már Phase 1-ben jelen van!)
- Tenant onboarding flow
- Teljes adatizoláció cégek között (GDPR)
- Iparági sablonok gyors beállításhoz

> ⚠️ A TenantId az első naptól minden entitáson kötelező, Phase 1-ben egyetlen fix értékkel.

---

## 2.4 Cross-cutting concerns (infrastruktúra szintű konfiguráció)

### Adatbázis (ADR-007)

Az adatbázis **PostgreSQL 16**, Npgsql EF Core provider-rel. Egyetlen migráció mappa (`Migrations/`), nincs provider-absztrakció.

```json
{
  "Database": {
    "ConnectionString": "Host=postgres;Database=rezilio;Username=rezilio;Password=..."
  }
}
```

> ⚠️ SaaS platform – mi irányítjuk az infrastruktúrát, az adatbázis mindig PostgreSQL. Nincs szükség pluggable provider absztrakcióra.

### Auth provider – Keycloak (ADR-012)

A hitelesítést **Keycloak** végzi (self-hosted, OpenID Connect). A REZILIO API csak Keycloak JWT tokeneket validál. Enterprise ügyfelek saját AD-ját vagy Entra ID-ját Keycloak Identity Brokering-en keresztül integráljuk – az alkalmazás kód nem változik.

```json
{
  "Authentication": {
    "Authority": "https://auth.rezilio.hu/realms/rezilio",
    "ClientId": "rezilio-api",
    "ClientSecret": "..."
  }
}
```

A Keycloak claim-ek egységes belső `AppClaims` konstansokra képeződnek:

| Keycloak claim | AppClaims konstans |
|---|---|
| `sub` | `app:user_id` |
| `email` | `app:email` |
| `tenant_id` (custom attr) | `app:tenant_id` |
| `realm_access.roles` | `app:roles` |

> ❌ Handler-ekben soha ne használj Keycloak-specifikus claim neveket – csak `AppClaims` konstansokat.

### Pénznem (ADR-006)

Tenant szintű konfiguráció – futásidőben változtatható:

- Minden pénzügyi érték `Money` value object (`Amount` + `CurrencyCode ISO 4217`)
- Tenant alapértelmezett pénznem: pl. `HUF`, `EUR`, `USD`
- Multi-currency esetén árfolyam-kezelés (Phase 2+)

### Lokalizáció / i18n (ADR-009)

Kétszintű lokalizáció:

**UI lokalizáció** – Next.js (next-intl):
- Fordítások JSON fájlokban: `messages/en.json`, `messages/hu.json` stb.
- Dinamikusan bővíthető: új nyelv = új JSON fájl, újraindítás nélkül
- Fallback nyelv: `en` (mindig kötelezően teljes)

**Tenant-szintű konfiguráció:**
- Alapértelmezett nyelv per tenant
- Engedélyezett nyelvek listája per tenant
- Felhasználói override: felhasználónként felülírható

**Adat lokalizáció** (Phase 2+, opcionális):
- Fordítható mezők (pl. Risk Title, Description) külön `*Translation` táblában
- Fallback: ha az adott nyelven nincs fordítás, az alap (tenant default) nyelv jelenik meg

### TenantSettings aggregate (összefoglaló)

```csharp
public class TenantSettings : AggregateRoot<TenantId>
{
    // Pénznem
    public CurrencyCode DefaultCurrency { get; }   // "HUF", "EUR", "USD"

    // Lokalizáció
    public string DefaultLanguage { get; }          // "hu", "en", "de"
    public IReadOnlyList<string> SupportedLanguages { get; }
    public string Locale { get; }                   // "hu-HU", "en-GB"
    public string TimeZone { get; }                 // "Central Europe Standard Time"
}
```

> Auth és DB konfiguráció **nem** kerül `TenantSettings`-be – azok infrastruktúra szintű, deployment-time beállítások.

---

## 3. Modulok

### 3.1 Modul kategóriák

| Kategória | Modulok | Elérhetőség |
|---|---|---|
| **Alap (mindig aktív)** | Identity, Licensing, Organization | Minden tervben |
| **Core (Basic csomag)** | RiskRegister, Assessment, Treatment | Basic+ |
| **Prémium I.** | Monitoring, Incidents | Professional+ |
| **Prémium II.** | Compliance, AdvancedReporting | Enterprise |
| **Enterprise** | ESG, AIInsights | Enterprise |

### 3.2 Modul deployment stratégia
- **Minden modul benne van egyetlen Docker image-ben**
- Aktiválás per-tenant feature flag / licensz alapján
- Deaktivált modul: UI "Upgrade szükséges" státuszt mutat, API 403-at ad vissza
- Trial: modul 14 napra aktiválható, lejárat után readonly mód

### 3.3 Előfizetési csomagok

**Basic**
- Organization (mindig aktív alap)
- RiskRegister, Assessment, Treatment
- Alap riportok
- 1 RiskDomain

**Professional**
- Minden Basic modul
- Monitoring & KRI-k
- Incidenskezelés
- Haladó riportok (PDF export)
- 3 RiskDomain párhuzamosan

**Enterprise**
- Minden Professional modul
- Compliance (ISO, GDPR, NIS2...)
- ESG modul
- AI-alapú kockázatelemzés
- Korlátlan RiskDomain
- API hozzáférés integrációkhoz

---

## 3.4 Organization modul – Master data és import

Az Organization modul az összes szervezeti referencia-adatot tárolja. Ezek az adatok a kockázatelemzés **alapjai** – minden kockázat valamelyik szervezeti entitáshoz kapcsolódik (pl. telephely, IT rendszer, ügyfél, beszállító).

### Importálható entitás típusok

| Entitás | Magyar neve | Importálható? | Form is? |
|---|---|---|---|
| `OrganizationalUnit` | Szervezeti egység (SZMSZ alapján) | ✅ Excel sablon | ✅ |
| `Location` | Telephely | ✅ Excel sablon | ✅ |
| `Customer` | Ügyfél | ✅ Excel sablon | ✅ |
| `Supplier` | Beszállító | ✅ Excel sablon | ✅ |
| `KeyPerson` | Kulcsszemély / pozíció | ✅ Excel sablon | ✅ |
| `ItSystem` | IT rendszer / alkalmazás | ✅ Excel sablon | ✅ |
| `BusinessProcess` | Üzleti folyamat | ✅ Excel sablon | ✅ |

### Excel import működése

**Sablon letöltés:** `GET /api/templates/{entityType}` → `.xlsx` fájl, előre definiált fejléccel, oszlop-magyarázatokkal és dropdown validációkkal (pl. kritikusság: Alacsony / Közepes / Magas / Kritikus).

**Feltöltés és validáció:** `POST /api/import/{entityType}` → soronkénti validáció, eredmény összefoglaló (X sor OK, Y sor hibás – miért).

**Megerősítés → végrehajtás:** A felhasználó látja az előnézetet, majd jóváhagyja. Hibás sorokat javíthatja, vagy átugorhatja.

**Könyvtár:** ClosedXML (MIT licensz, open-source)

### Entitások kulcsmezői

**OrganizationalUnit** (SZMSZ-ből): Megnevezés, Szülő egység (hierarchia), Vezető, Telephely, Típus (Osztály / Főosztály / Divízió / Csoport)

**Location** (Telephely): Megnevezés, Cím, Típus (Székhely / Telephely / Adatközpont / Raktár), Ország, Felelős, Kritikusság

**Customer** (Ügyfél): Cégnév, Adószám, Kapcsolattartó, Email, Telefon, Ügyfélkategória, Kritikusság

**Supplier** (Beszállító): Cégnév, Adószám, Kapcsolattartó, Email, Telephely ország, Kategória (IT / Logisztika / Gyártás / Szolgáltatás), Kritikusság, Szerződés lejárata

**KeyPerson** (Kulcsszemély): Név, Pozíció, Szervezeti egység, Email, Helyettes neve, Speciális tudás/szerepkör

**ItSystem** (IT rendszer): Megnevezés, Típus (ERP / CRM / Infrastruktúra / Alkalmazás), Felelős, Gyártó, Kritikusság, Üzemeltetési modell (On-premise / Cloud / Hybrid), Kapcsolódó folyamatok

**BusinessProcess** (Üzleti folyamat): Megnevezés, Szervezeti egység, Folyamat-felelős, Kritikusság, Kapcsolódó IT rendszerek, Helyettesíthetőség (Igen / Korlátozott / Nem)

### Import infrastruktúra (domain)

```csharp
// Modules/Organization/Domain/
public class ImportJob : AggregateRoot<ImportJobId>
{
    public TenantId TenantId { get; }
    public EntityType EntityType { get; }      // OrganizationalUnit, Location, stb.
    public ImportStatus Status { get; }        // Pending, Processing, Completed, Failed
    public int TotalRows { get; }
    public int SuccessRows { get; }
    public int ErrorRows { get; }
    public IReadOnlyList<ImportRowResult> Results { get; }
}

public record ImportRowResult(int RowNumber, bool Success, string? ErrorMessage);

public enum EntityType
{
    OrganizationalUnit, Location, Customer,
    Supplier, KeyPerson, ItSystem, BusinessProcess
}
```

---

## 4. Domain modell áttekintés

### 4.1 Kulcs aggregátok

| Aggregate | Modul | Leírás |
|---|---|---|
| `OrganizationalUnit` | Organization | Szervezeti egység (SZMSZ-ből) |
| `Location` | Organization | Telephely |
| `Customer` | Organization | Ügyfél |
| `Supplier` | Organization | Beszállító |
| `KeyPerson` | Organization | Kulcsszemély / pozíció |
| `ItSystem` | Organization | IT rendszer / alkalmazás |
| `BusinessProcess` | Organization | Üzleti folyamat |
| `ImportJob` | Organization | Import művelet nyilvántartása |
| `Risk` | RiskRegister | Kockázat életciklusa |
| `Assessment` | Assessment | Értékelési rekord |
| `TreatmentPlan` | Treatment | Kezelési terv + kontrollok |
| `KRI` | Monitoring | Kockázati indikátor |
| `Incident` | Incidents | Bekövetkezett kockázati esemény |
| `ComplianceFramework` | Compliance | Szabványi keretrendszer |
| `TenantLicense` | Licensing | Tenant előfizetés és modul-hozzáférés |

### 4.2 Kulcs value object-ek

- `TenantId` – minden entitáson kötelező
- `RiskScore` – likelihood × impact
- `Likelihood`, `Impact` – 1-5 skála
- `RiskCategory` – IT / Financial / ESG / Operational / Compliance
- `ModuleType` – enum az összes modulhoz
- `Money` – `Amount (decimal)` + `CurrencyCode (ISO 4217)`
- `CurrencyCode` – ISO 4217 kód: `HUF`, `EUR`, `USD` stb.
- `LanguageCode` – BCP 47: `hu`, `en`, `de` stb.

### 4.3 Kulcs domain event-ek

| Event | Küldő | Fogadó(k) |
|---|---|---|
| `RiskCreated` | RiskRegister | Assessment, Monitoring, Reporting |
| `RiskScoreChanged` | Assessment | Monitoring, Reporting |
| `AssessmentCompleted` | Assessment | Treatment, Reporting |
| `ControlStatusChanged` | Treatment | Monitoring, Reporting |
| `KRIThresholdBreached` | Monitoring | Incidents, Reporting |
| `IncidentReported` | Incidents | RiskRegister, Reporting |
| `ModuleActivated` | Licensing | érintett modulok |
| `TrialExpired` | Licensing | érintett modulok |

---

## 5. Funkcionális területek (Risk Domainok)

A platform az alábbi kockázati területeket képes kiszolgálni (iparági sablon alapján):

- **IT / Kiberbiztonság** – ISO 27001, NIST CSF
- **Pénzügyi / Treasury** – Basel III/IV, VaR
- **Operacionális** – folyamat, ellátási lánc
- **Jogi / Megfelelőségi** – GDPR, NIS2, DORA
- **Projekt** – határidő, költség, scope
- **ESG / Fenntarthatósági** – CSRD, TCFD
- **Egészségügyi** – betegbiztonság, klinikai

---

## 6. Megvalósítási fázisok

| Fázis | Tartalom | Időtartam |
|---|---|---|
| **Phase 0** | Infra, Keycloak, Licensing alap | 4 hét |
| **Phase 1 (MVP)** | RiskRegister, Assessment, Treatment | 8 hét |
| **Phase 2** | Monitoring, Incidents | 8 hét |
| **Phase 3** | Compliance, AdvancedReporting, ESG alap | 8 hét |
| **Phase 4** | AIInsights, B2B Multitenancy, Hardening | 6 hét |

---

## 7. Nem funkcionális követelmények

- **Biztonság:** Keycloak JWT auth, modulszintű jogosultság-ellenőrzés minden endpoint-on
- **Auditálhatóság:** minden entitásváltozás naplózva (ki, mikor, mit)
- **Teljesítmény:** listaoldalak < 500ms, riport generálás async
- **Skálázhatóság:** Hetzner VPS + Docker, horizontális skálázás Load Balancer-rel
- **GDPR:** tenant adatok teljes izoláltságban, törölhetők
- **Adatbázis:** PostgreSQL 16, Npgsql, egyetlen migráció mappa
- **Auth:** Keycloak self-hosted, OIDC – kódmódosítás nélkül bővíthető enterprise federation
- **Többnyelvűség:** UI és adat szintű i18n, dinamikusan bővíthető nyelvekkel
- **Multi-currency:** `Money` value object, tenant szintű pénznem konfiguráció
- **SSL:** Let's Encrypt, Traefik automatikus megújítás
- **Observability:** OpenTelemetry + Grafana + Prometheus + Loki (self-hosted)

---

*Ez a dokumentum folyamatosan bővül a tervezési sessionok alapján.*
