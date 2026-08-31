using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.UpdateTenantSettings;

public sealed class UpdateTenantSettingsHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/settings")]
    [Authorize]
    public async Task<IResult> Handle(UpdateTenantSettingsCommand command, CancellationToken ct)
    {
        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, ct);

        if (settings is null)
        {
            settings = TenantSettings.Create(
                tenantContext.TenantId,
                new CurrencyCode(command.DefaultCurrency),
                new LanguageCode(command.DefaultLanguage),
                command.Locale,
                command.TimeZone);
            db.TenantSettings.Add(settings);
        }
        else
        {
            settings.Update(
                new CurrencyCode(command.DefaultCurrency),
                new LanguageCode(command.DefaultLanguage),
                command.Locale,
                command.TimeZone);
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
