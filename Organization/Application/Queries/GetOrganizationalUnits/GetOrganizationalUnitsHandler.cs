namespace Rezilio.Modules.Organization.Application.Queries.GetOrganizationalUnits;

public sealed record OrganizationalUnitResponse(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentId,
    string? Description);

public sealed class GetOrganizationalUnitsHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/units")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var units = await db.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Name)
            .Select(u => new OrganizationalUnitResponse(u.Id, u.Name, u.Code, u.ParentId, u.Description))
            .ToListAsync(ct);

        return Results.Ok(units);
    }
}
