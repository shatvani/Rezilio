using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Application.Queries.GetOrganizationalUnits;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Queries.GetOrganizationalUnit;

public sealed class GetOrganizationalUnitHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/units/{id}")]
    [Authorize]
    public async Task<IResult> Handle(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var unit = await db.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new OrganizationalUnitResponse(u.Id, u.Name, u.Code, u.ParentId, u.Description))
            .FirstOrDefaultAsync(ct);

        if (unit is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(unit);
    }
}
