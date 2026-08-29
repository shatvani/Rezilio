using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organization.Application.Queries.GetItSystemById;
using Rezilio.Modules.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Organization.Application.Queries.GetItSystemsByTenant;

public sealed class GetItSystemsByTenantHandler
{
    private readonly OrganizationDbContext _db;

    public GetItSystemsByTenantHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverineGet("/api/organization/it-systems")]
    [Authorize]
    public async Task<IResult> Handle([FromQuery] Guid tenantId, CancellationToken ct)
    {
        List<ItSystemDto> systems = await _db.ItSystems
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Code)
            .Select(s => new ItSystemDto(
                s.Id, s.TenantId, s.Code, s.Name, s.Type, s.HostingType,
                s.CriticalityLevel, s.Vendor, s.Version, s.OwnerId, s.SupportedOrgUnitIds))
            .ToListAsync(ct);

        return Results.Ok(systems);
    }
}
