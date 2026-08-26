using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rezilio.Modules.Organization.Infrastructure;
using Wolverine.Http;

namespace Rezilio.Modules.Organization.Application.Queries.GetKeyPersonsByTenant;

public sealed class GetKeyPersonsByTenantHandler(OrganizationDbContext db)
{
    [WolverineGet("/api/organization/key-persons")]
    [Authorize]
    public async Task<IResult> Handle([FromQuery] Guid tenantId, CancellationToken ct)
    {
        var keyPersons = await db.KeyPersons
            .AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .OrderBy(k => k.Name)
            .Select(k => new
            {
                k.Id,
                k.Name,
                k.Title,
                k.Department,
                k.OrgUnitId,
                k.Email,
                k.Phone,
                k.BackupPersonName,
                k.Description
            })
            .ToListAsync(ct);

        return Results.Ok(keyPersons);
    }
}
