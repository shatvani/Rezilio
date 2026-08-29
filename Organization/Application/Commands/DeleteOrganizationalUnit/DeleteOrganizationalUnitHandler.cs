namespace Rezilio.Modules.Organization.Application.Commands.DeleteOrganizationalUnit;

public sealed class DeleteOrganizationalUnitHandler(OrganizationDbContext db)
{
    [WolverineDelete("/api/organization/units/{id}")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        OrganizationalUnit? unit = await db.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit is null)
        {
            return Results.NotFound();
        }

        db.OrganizationalUnits.Remove(unit);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
