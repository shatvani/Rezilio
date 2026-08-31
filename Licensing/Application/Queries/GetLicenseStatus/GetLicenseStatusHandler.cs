namespace Rezilio.Modules.Licensing.Application.Queries.GetLicenseStatus;

public sealed class GetLicenseStatusHandler(LicensingDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/licensing/status/{tenantId}")]
    [Authorize]
    public async Task<LicenseStatusResult?> Handle(Guid tenantId, CancellationToken ct)
    {
        TenantLicense? license = await db.Licenses
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == tenantContext.TenantId, ct);

        if (license is null) { return null; }

        return new LicenseStatusResult(
            license.TenantId,
            license.Plan.ToString(),
            license.PlanExpiresAt,
            license.ModuleAccesses
                   .Select(m => new ModuleAccessResult(
                       m.Module.ToString(),
                       m.IsActive,
                       m.TrialEndsAt,
                       m.IsAccessible))
                   .ToList());
    }
}
