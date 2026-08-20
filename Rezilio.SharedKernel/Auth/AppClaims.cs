namespace Rezilio.SharedKernel.Auth;

/// <summary>
/// Normalizált claim nevek, amelyeket a Keycloak JWT-ből a KeycloakClaimsTransformation állít elő.
/// Handler-ekben kizárólag ezek a konstansok használhatók – soha nem a nyers Keycloak claim nevek.
/// </summary>
public static class AppClaims
{
    /// <summary>A felhasználó egyedi azonosítója (Keycloak user UUID).</summary>
    public const string UserId = "app:user_id";

    /// <summary>A tenant egyedi azonosítója (GUID string).</summary>
    public const string TenantId = "app:tenant_id";

    /// <summary>A felhasználó e-mail címe.</summary>
    public const string Email = "app:email";

    /// <summary>A felhasználóhoz rendelt szerepkörök (multivalued).</summary>
    public const string Roles = "app:roles";
}
