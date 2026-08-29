namespace Rezilio.Modules.Organization.Application.Commands.UpdateOrganizationalUnit;

public sealed class UpdateOrganizationalUnitHandler(OrganizationDbContext db)
{
    [WolverinePut("/api/organization/units/{id}")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid id,
        UpdateOrganizationalUnitCommand command,
        CancellationToken ct)
    {
        OrganizationalUnit? unit = await db.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit is null)
        {
            return Results.NotFound();
        }

        bool codeExists = await db.OrganizationalUnits
            .AnyAsync(u => u.TenantId == unit.TenantId
                        && u.Code == command.Code.Trim().ToUpperInvariant()
                        && u.Id != id, ct);

        if (codeExists)
        {
            return Results.Conflict($"Organizational unit with code '{command.Code}' already exists.");
        }

        unit.Update(command.Name, command.Code, command.ParentId, command.Description);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
