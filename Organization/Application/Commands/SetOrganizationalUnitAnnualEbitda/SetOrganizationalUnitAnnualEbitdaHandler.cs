using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Application.Commands.SetOrganizationalUnitAnnualEbitda;

public sealed class SetOrganizationalUnitAnnualEbitdaHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverinePost("/api/organization/organizational-units/{OrgUnitId}/annual-ebitda")]
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Handle(SetOrganizationalUnitAnnualEbitdaCommand command, CancellationToken ct)
    {
        var orgUnit = await db.OrganizationalUnits
            .FirstOrDefaultAsync(o => o.Id == command.OrgUnitId && o.TenantId == tenantContext.TenantId, ct);

        if (orgUnit is null)
        {
            return Results.NotFound();
        }

        var ebitda = command.Amount is { } amount
            ? new Money(amount, new CurrencyCode(command.Currency!))
            : null;

        orgUnit.SetAnnualEbitda(ebitda);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
