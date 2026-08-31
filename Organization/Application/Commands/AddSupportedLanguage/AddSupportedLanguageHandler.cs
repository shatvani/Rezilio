using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.AddSupportedLanguage;

public sealed class AddSupportedLanguageHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/settings/{tenantId}/languages")]
    [Authorize]
    public async Task<IResult> Handle(
        Guid tenantId,
        AddSupportedLanguageCommand command,
        CancellationToken ct)
    {
        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, ct);

        if (settings is null) { return Results.NotFound(); }

        settings.AddSupportedLanguage(new LanguageCode(command.LanguageCode));
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
