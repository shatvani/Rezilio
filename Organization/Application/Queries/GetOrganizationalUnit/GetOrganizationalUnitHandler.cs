using Rezilio.Modules.Organization.Application.Queries.GetOrganizationalUnits;

namespace Rezilio.Modules.Organization.Application.Queries.GetOrganizationalUnit;

public sealed class GetOrganizationalUnitHandler(OrganizationDbContext db, ITenantContext tenantContext)
{
    [WolverineGet("/api/organization/units/{id}")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var unit = await db.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.Id == id && u.TenantId == tenantContext.TenantId)
            .Select(u => new OrganizationalUnitResponse(u.Id, u.Name, u.Code, u.ParentId, u.Description))
            .FirstOrDefaultAsync(ct);

        if (unit is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(unit);
    }
}
