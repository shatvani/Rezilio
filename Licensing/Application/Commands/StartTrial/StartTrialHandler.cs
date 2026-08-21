using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Licensing.Domain;
using Rezilio.Modules.Licensing.Infrastructure;

namespace Rezilio.Modules.Licensing.Application.Commands.StartTrial;

public sealed class StartTrialHandler
{
    public static async Task Handle(
        StartTrialCommand command,
        LicensingDbContext db,
        CancellationToken ct)
    {
        TenantLicense license = await db.Licenses
            .FirstOrDefaultAsync(l => l.TenantId == command.TenantId, ct)
            ?? throw new InvalidOperationException($"Nincs licensz a(z) {command.TenantId} tenanthoz.");

        license.StartTrial(command.Module);
        await db.SaveChangesAsync(ct);
    }
}
