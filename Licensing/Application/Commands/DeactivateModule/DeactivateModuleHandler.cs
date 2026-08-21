using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Licensing.Domain;
using Rezilio.Modules.Licensing.Infrastructure;

namespace Rezilio.Modules.Licensing.Application.Commands.DeactivateModule;

public sealed class DeactivateModuleHandler
{
    public static async Task Handle(
        DeactivateModuleCommand command,
        LicensingDbContext db,
        CancellationToken ct)
    {
        TenantLicense license = await db.Licenses
            .FirstOrDefaultAsync(l => l.TenantId == command.TenantId, ct)
            ?? throw new InvalidOperationException($"Nincs licensz a(z) {command.TenantId} tenanthoz.");

        license.DeactivateModule(command.Module);
        await db.SaveChangesAsync(ct);
    }
}
