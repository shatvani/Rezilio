using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Licensing.Domain;
using Rezilio.Modules.Licensing.Infrastructure;

namespace Rezilio.Modules.Licensing.Application.Commands.ActivateModule;

public sealed class ActivateModuleHandler
{
    public static async Task Handle(
        ActivateModuleCommand command,
        LicensingDbContext db,
        CancellationToken ct)
    {
        TenantLicense license = await db.Licenses
            .FirstOrDefaultAsync(l => l.TenantId == command.TenantId, ct)
            ?? throw new InvalidOperationException($"Nincs licensz a(z) {command.TenantId} tenanthoz.");

        license.ActivateModule(command.Module);
        await db.SaveChangesAsync(ct);
    }
}
