namespace Rezilio.SharedKernel.Multitenancy;

/// <summary>
/// Phase 1 implementáció: minden kéréshez egyetlen fix TenantId-t ad vissza.
/// A dev felhasználók tenant_id attribútuma is erre az értékre van beállítva a Keycloak realm-ben.
/// </summary>
public sealed class FixedTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001");
}
