using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Licensing.Domain;
using Rezilio.Modules.Licensing.Infrastructure;

namespace Rezilio.Modules.Licensing.Application.Services;

internal sealed class ModuleAccessChecker : IModuleAccessChecker
{
    private readonly LicensingDbContext _db;

    public ModuleAccessChecker(LicensingDbContext db) => _db = db;

    public async Task<bool> IsModuleActiveAsync(ModuleType module, Guid tenantId, CancellationToken ct = default)
    {
        TenantLicense? license = await _db.Licenses
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.TenantId == tenantId, ct);

        return license?.IsModuleActive(module) ?? false;
    }
}
