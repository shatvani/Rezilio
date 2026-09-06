using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.SetTenantDefaultAnnualEbitda;

public sealed class SetTenantDefaultAnnualEbitdaHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/settings/default-annual-ebitda")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Handle(SetTenantDefaultAnnualEbitdaCommand command, CancellationToken ct)
    {
        var settings = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantContext.TenantId, ct);

        if (settings is null)
        {
            // A TenantSettings-nek már léteznie kell (UpdateTenantSettings hozza létre) —
            // önmagában az EBITDA-alapérték nem hozhat létre tenant-beállítást.
            return Results.NotFound("A tenant beállításai még nincsenek inicializálva.");
        }

        var ebitda = command.Amount is { } amount
            ? new Money(amount, new CurrencyCode(command.Currency!))
            : null;

        settings.SetDefaultAnnualEbitda(ebitda);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
