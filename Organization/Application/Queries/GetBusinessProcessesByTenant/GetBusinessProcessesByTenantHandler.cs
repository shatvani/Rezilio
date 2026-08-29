using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organization.Application.Queries.GetBusinessProcessById;
using Rezilio.Modules.Organization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Organization.Application.Queries.GetBusinessProcessesByTenant;

public sealed class GetBusinessProcessesByTenantHandler
{
    private readonly OrganizationDbContext _db;

    public GetBusinessProcessesByTenantHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    [WolverineGet("/api/organization/business-processes")]
    [Authorize]
    public async Task<IResult> Handle([FromQuery] Guid tenantId, CancellationToken ct)
    {
        List<BusinessProcessDto> list = await _db.BusinessProcesses
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.Code)
            .Select(b => new BusinessProcessDto(
                b.Id, b.TenantId, b.Code, b.Name, b.Category, b.CriticalityLevel,
                b.OwnerId, b.OrgUnitId, b.MaxTolerableDowntimeMinutes,
                b.RecoveryTimeObjectiveMinutes, b.DependsOnSystemIds))
            .ToListAsync(ct);

        return Results.Ok(list);
    }
}
