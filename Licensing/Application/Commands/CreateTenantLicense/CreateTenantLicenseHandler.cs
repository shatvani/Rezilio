using Rezilio.Modules.Licensing.Domain;
using Rezilio.Modules.Licensing.Infrastructure;

namespace Licensing.Application.Commands.CreateTenantLicense;

public sealed class CreateTenantLicenseHandler
{
    public static async Task Handle(
        CreateTenantLicenseCommand command,
        LicensingDbContext db,
        CancellationToken ct)
    {
        var license = TenantLicense.Create(command.TenantId, command.Plan, command.ExpiresAt);
        db.Licenses.Add(license);
        await db.SaveChangesAsync(ct);
    }
}
