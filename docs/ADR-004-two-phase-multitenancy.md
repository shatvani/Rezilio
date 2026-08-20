# ADR-004 – Kétfázisú multitenancy stratégia

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

A platform két különböző multitenancy dimenziót kell kezeljen:
1. **B2B:** több független cég (tenant) ugyanazt a platformot használja
2. **Multi-domain:** egy cégen belül több kockázati terület (IT, pénzügyi, ESG stb.)

A kettő különböző technikai megoldást igényel. Az MVP-hez a B2B SaaS multitenancy túl komplex, de utólag nehéz bevezetni.

## Döntés

**Kétfázisú megközelítés:**

### Phase 1 – Single Tenant + Multi Risk Domain
- Egyetlen fix `TenantId` érték az egész Phase 1-ben
- `TenantId` azonban minden entitáson kötelező mező az első naptól
- Egy cégen belüli többterületes kezelés: `RiskDomain` entitással megoldva
- Felhasználók `RiskDomain` szintű jogosultsággal rendelkeznek

### Phase 2 – Teljes B2B SaaS Multitenancy
- Per-request `TenantId` resolution (JWT claim-ből)
- Teljes adatizoláció cégek között
- Tenant onboarding flow, iparági sablonok
- `ITenantContext` interface már Phase 1-ben létezik, Phase 2-ben valódi implementációt kap

## Indoklás

| Szempont | Értékelés |
|---|---|
| **MVP sebesség** | ✅ Phase 1 jóval egyszerűbb és gyorsabb |
| **Refactor kockázat** | ✅ TenantId mindenhol → Phase 2 csak implementáció csere |
| **GDPR felkészültség** | ✅ Az adatizoláció alapjai Phase 1-től adottak |
| **Üzleti kockázat** | ✅ Phase 1 önmagában értékesíthető, B2B SaaS opcionális |

## Technikai implikációk

```csharp
// Phase 1 – fix tenant
public class FixedTenantContext : ITenantContext
{
    public TenantId Current => TenantId.Default; // fix érték
}

// Phase 2 – JWT-ből
public class JwtTenantContext : ITenantContext
{
    public TenantId Current => TenantId.From(_httpContextAccessor.HttpContext
        .User.FindFirstValue("tenant_id"));
}
// DI-ban csak az implementációt cseréljük – minden Handler változatlan marad
```

## Következmények

- Minden EF Core query-hez automatikus `TenantId` szűrő (Global Query Filter)
- `AggregateRoot` base class kikényszeríti a `TenantId` meglétét
- Phase 1-ben a fejlesztő sosem gondolkodik tenant izoláción – automatikus
- Phase 2 bevezetésekor nincs szükség domain logika módosítására
