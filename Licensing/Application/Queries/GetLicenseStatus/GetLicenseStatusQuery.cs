namespace Rezilio.Modules.Licensing.Application.Queries.GetLicenseStatus;

public sealed record GetLicenseStatusQuery(Guid TenantId);

public sealed record LicenseStatusResult(
    Guid TenantId,
    string Plan,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<ModuleAccessResult> Modules);

public sealed record ModuleAccessResult(
    string Module,
    bool IsActive,
    DateTimeOffset? TrialEndsAt,
    bool IsAccessible);
