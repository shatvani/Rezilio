namespace Rezilio.Modules.Organization.Application.Commands.DeleteOrganizationalUnit;

public sealed class DeleteOrganizationalUnitHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineDelete("/api/organization/units/{id}")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        OrganizationalUnit? unit = await db.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantContext.TenantId, ct);

        if (unit is null)
        {
            return Results.NotFound();
        }

        db.OrganizationalUnits.Remove(unit);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
