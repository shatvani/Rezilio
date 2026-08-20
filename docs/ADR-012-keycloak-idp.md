# ADR-012 – Keycloak mint központi Identity Provider (felváltja ADR-008)

**Dátum:** 2026-08-19  
**Státusz:** Elfogadva  
**Felváltja:** ADR-008 (Pluggable auth provider)  
**Döntéshozó:** Projekt architekt

---

## Kontextus

Az ADR-008 "pluggable auth provider" megközelítést definiált: az alkalmazás kódban kellett volna implementálni LocalSql, Active Directory, Azure Entra ID, Keycloak és OpenID Connect providereket. Ez szükségtelenül komplex egy SaaS platformnál, ahol az infrastruktúrát mi irányítjuk.

A kérdés: hogyan kezeljük a felhasználók hitelesítését, beleértve azt is, hogy egyes enterprise ügyfeleknek saját AD-juk vagy Entra ID-juk van?

---

## Döntés

**Keycloak self-hosted, mint az egyetlen Identity Provider, amellyel a REZILIO kommunikál.**

A REZILIO alkalmazás csak Keycloak OIDC tokeneket validál. Ha egy ügyfélnek saját vállalati identity megoldása van (AD, Entra ID, SAML IdP), azt Keycloak Identity Brokering-en keresztül integráljuk – az alkalmazás kódja nem változik.

---

## Indoklás

| Szempont | Pluggable auth (ADR-008) | Keycloak mint IdP |
|---|---|---|
| **Kódkomplexitás** | ❌ 4–5 provider implementáció | ✅ 1 OIDC integráció |
| **Enterprise federation** | ❌ REZILIO kódban | ✅ Keycloak kezeli (LDAP, SAML, OIDC) |
| **SaaS pattern** | ❌ On-premise szemlélet | ✅ Standard SaaS megközelítés |
| **Open-source** | Részben | ✅ Keycloak Apache 2.0 |
| **Self-hosted** | ✅ | ✅ |
| **Admin felület** | Manuális fejlesztés | ✅ Keycloak Admin Console |
| **MFA, social login** | Fejleszteni kell | ✅ Keycloak beépített |

---

## Architektúra

```
Enterprise ügyfél (AD / Entra ID / SAML)
         ↓  LDAP / OIDC / SAML
      [Keycloak]  ←── Identity Brokering
         ↓  OpenID Connect (JWT)
    [REZILIO API]
         ↓  AppClaims normalizálás
    [Handler-ek]
```

A REZILIO soha nem látja a forrás identity rendszert – csak a Keycloak által kiadott, normalizált JWT tokent kapja.

---

## Keycloak konfiguráció

### Realm struktúra

- `master` realm – csak Keycloak admin (nem REZILIO)
- `rezilio` realm – REZILIO felhasználók és kliensek
- Enterprise ügyfélnél: Identity Provider hozzáadása a `rezilio` realm-ben (LDAP vagy OIDC broker)

### Client konfiguráció

```
Client ID:        rezilio-api
Protocol:         openid-connect
Access type:      confidential
Valid redirect:   https://app.rezilio.hu/*
```

### Roles (Keycloak realm roles)

- `Admin` – rendszergazda
- `RiskManager` – kockázatkezelő
- `RiskOwner` – kockázat tulajdonos
- `Auditor` – csak olvasás, audit
- `Executive` – vezető dashboard
- `Viewer` – csak megtekintés

---

## Claims normalizálás

A Keycloak token tartalmaz standard és custom claim-eket. A REZILIO ezeket egységes `AppClaims` konstansokra képezi le egy `IClaimsTransformation` implementációban:

```csharp
// SharedKernel/Auth/KeycloakClaimsTransformation.cs
public class KeycloakClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Keycloak standard claims → AppClaims
        MapClaim(identity, "sub",              AppClaims.UserId);
        MapClaim(identity, "email",            AppClaims.Email);
        MapClaim(identity, "tenant_id",        AppClaims.TenantId);   // Keycloak custom attribute
        MapKeycloakRoles(identity);                                    // realm_access.roles → app:roles

        return Task.FromResult(principal);
    }
}

// SharedKernel/Auth/AppClaims.cs
public static class AppClaims
{
    public const string UserId   = "app:user_id";
    public const string TenantId = "app:tenant_id";
    public const string Email    = "app:email";
    public const string Roles    = "app:roles";
}
```

> ❌ Handler-ekben soha ne használj Keycloak-specifikus claim neveket (`sub`, `realm_access` stb.) – csak `AppClaims` konstansokat.

---

## Tenant ID kezelés Keycloak-ban

Minden REZILIO felhasználóhoz a Keycloak admin felületen (vagy API-n) beállítható egy `tenant_id` user attribute. Ez kerül a JWT tokenbe mapper-en keresztül, és a `KeycloakClaimsTransformation` az `AppClaims.TenantId`-re képezi.

Multi-tenant SaaS esetén: az onboarding flow automatikusan beállítja az új tenant felhasználóinak `tenant_id` értékét.

---

## Enterprise Identity Federation (Keycloak Identity Brokering)

Ha egy enterprise ügyfélnek saját AD-ja vagy Entra ID-ja van:

1. Keycloak Admin Console-ban Identity Provider hozzáadása a `rezilio` realm-ben
2. Protokoll: LDAP (AD) / OpenID Connect (Entra ID) / SAML 2.0
3. User mapper: vállalati attribútumok → Keycloak user profile
4. REZILIO kód **nem változik** – ugyanazt a JWT tokent kapja

**REZILIO kód nem tud arról, hogy az ügyfél AD-t vagy Entra ID-t használ.**

---

## Dev környezet

Lokális fejlesztéshez Keycloak dev realm Docker Compose-ban:

```yaml
# docker-compose.dev.yml
services:
  keycloak:
    image: quay.io/keycloak/keycloak:25
    command: start-dev --import-realm
    environment:
      KC_BOOTSTRAP_ADMIN_USERNAME: admin
      KC_BOOTSTRAP_ADMIN_PASSWORD: admin
    volumes:
      - ./keycloak/rezilio-realm-dev.json:/opt/keycloak/data/import/realm.json
    ports:
      - "8080:8080"
```

A `rezilio-realm-dev.json` a repóban van – tartalmazza a dev user-eket, role-okat és a client konfigurációt. **Soha nem tartalmaz production secret-et.**

---

## Következmények

- Az ADR-008-ban tervezett pluggable auth provider implementációk (LocalSql, AD, AzureEntraID stb.) **nem készülnek el** – feleslegesek
- Story 0.5 radikálisan egyszerűsödik: csak Keycloak OIDC JWT validáció + claims normalizálás
- Enterprise ügyfelek identity integrációja Keycloak Admin-on keresztül kezelhető, fejlesztői beavatkozás nélkül
- MFA, social login, brute-force protection: Keycloak beépített funkciók, nem kell implementálni
- Keycloak saját PostgreSQL adatbázist használ (különálló DB a REZILIO app DB-jétől)
