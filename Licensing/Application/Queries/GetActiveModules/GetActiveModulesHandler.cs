namespace Rezilio.Modules.Licensing.Application.Queries.GetActiveModules;

public sealed class GetActiveModulesHandler(LicensingDbContext db)
{
    [WolverineGet("/api/licensing/modules/{tenantId}")]
    [Authorize]
    public async Task<IReadOnlyList<string>> Handle(Guid tenantId, CancellationToken ct)
    {
        TenantLicense? license = await db.Licenses
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId, ct);

        if (license is null) { return []; }

        return license.ModuleAccesses
            .Where(m => m.IsAccessible)
            .Select(m => m.Module.ToString())
            .ToList();
    }
}
