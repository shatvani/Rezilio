# ADR-008 – Pluggable auth provider, claims normalizálás

**Dátum:** 2026-08-16  
**Státusz:** Elfogadva  
**Döntéshozó:** Projekt architekt

---

## Kontextus

Különböző ügyfelek eltérő identity infrastruktúrát használnak: egyes nagyvállalatok Active Directory-ra vagy Azure Entra ID-ra standardizáltak, mások Keycloak-ot futtatnak, kisebb szervezetek SQL-alapú auth-ot igényelnek. Az auth providernek deployment-time konfigurációból cserélhetőnek kell lennie.

## Döntés

**Pluggable auth provider** architektúra `IAuthProviderConfiguration` interface alapján, egységes belső claim modellel (`AppClaims`).

```json
// appsettings.json
{
  "Authentication": {
    "Provider": "AzureEntraID",
    "Settings": {
      "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
      "ClientId": "...",
      "ClientSecret": "...",
      "Audience": "api://..."
    }
  }
}
```

```csharp
// Interface
public interface IAuthProviderConfiguration
{
    void Configure(AuthenticationBuilder builder, IConfiguration settings);
    void RegisterServices(IServiceCollection services, IConfiguration settings);
}

// Implementációk
public class LocalSqlAuthProvider     : IAuthProviderConfiguration { ... }
public class AzureEntraIdAuthProvider : IAuthProviderConfiguration { ... }
public class KeycloakAuthProvider     : IAuthProviderConfiguration { ... }
public class ActiveDirectoryProvider  : IAuthProviderConfiguration { ... }
public class GenericOidcAuthProvider  : IAuthProviderConfiguration { ... }
```

## Claims normalizálás

Minden provider különböző claim neveket használ. Egységes belső modell:

```csharp
// SharedKernel – AppClaims konstansok
public static class AppClaims
{
    public const string UserId   = "app:user_id";
    public const string TenantId = "app:tenant_id";
    public const string Email    = "app:email";
    public const string Roles    = "app:roles";
    public const string Language = "app:language";
}

// Minden provider saját ClaimsTransformer-t regisztrál
public class AzureEntraClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // "oid" → AppClaims.UserId
        // "tid" → AppClaims.TenantId (Phase 2-ben)
        // "email" → AppClaims.Email
        // "roles" → AppClaims.Roles
    }
}

public class KeycloakClaimsTransformer : IClaimsTransformation
{
    // "sub" → AppClaims.UserId
    // "realm_access.roles" → AppClaims.Roles
}
```

## Támogatott providerek

| Provider | Protokoll | Tipikus use case |
|---|---|---|
| `LocalSql` | ASP.NET Core Identity | Dev, kis szervezet |
| `AzureEntraID` | OpenID Connect / OAuth2 | Microsoft-alapú nagyvállalat |
| `Keycloak` | OpenID Connect / OAuth2 | On-premise SSO igény |
| `ActiveDirectory` | LDAP / Windows Auth | Hagyományos nagyvállalat |
| `OpenIdConnect` | OpenID Connect | Generikus OIDC provider |

## Handler-ek auth-függetlensége

```csharp
// Handler – csak AppClaims konstansokat használ, soha provider-specifikus claim nevet
public class GetCurrentUserHandler
{
    public async Task<UserDto> Handle(GetCurrentUserQuery query, ICurrentUserContext user)
    {
        var userId = user.UserId;   // AppClaims.UserId alapján feloldva
        var email  = user.Email;    // AppClaims.Email alapján feloldva
        // ...
    }
}
```

## Következmények

- A Handler-ek teljesen auth-provider-agnosztikusak
- Provider csere: `appsettings.json` módosítás + újraindítás, kódmódosítás nélkül
- `LocalSql` provider esetén a `RegisterUser` endpoint aktív; SSO providerek esetén inaktív
- Minden provider-váltást integrációs teszttel kell validálni
