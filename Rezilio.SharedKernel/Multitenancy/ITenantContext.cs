namespace Rezilio.SharedKernel.Multitenancy;

/// <summary>
/// Az aktuális HTTP kérés tenant kontextusa.
/// Phase 1: egyetlen fix TenantId van (FixedTenantContext).
/// Phase 2: per-request TenantId resolution JWT claim alapján.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
