namespace Rezilio.Modules.Organization.Application.Queries.GetTenantSettings;

public sealed class GetTenantSettingsHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/settings/{tenantId}")]
    [Authorize]
    public async Task<IResult> Handle(Guid tenantId, CancellationToken ct)
    {
        var settings = await db.TenantSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, ct);

        if (settings is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new TenantSettingsResult(
            settings.TenantId,
            settings.DefaultCurrency.Value,
            settings.DefaultLanguage.Value,
            settings.Locale,
            settings.TimeZone,
            settings.SupportedLanguages.Select(l => l.Value).ToList()));
    }
}
