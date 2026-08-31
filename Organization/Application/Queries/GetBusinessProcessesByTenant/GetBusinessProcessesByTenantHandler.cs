using Rezilio.Modules.Organization.Application.Queries.GetBusinessProcessById;

namespace Rezilio.Modules.Organization.Application.Queries.GetBusinessProcessesByTenant;

public sealed class GetBusinessProcessesByTenantHandler
{
    private readonly OrganizationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetBusinessProcessesByTenantHandler(OrganizationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [WolverineGet("/api/organization/business-processes")]
    [Authorize]
    public async Task<IResult> Handle(CancellationToken ct)
    {
        List<BusinessProcessDto> list = await _db.BusinessProcesses
            .Where(b => b.TenantId == _tenantContext.TenantId)
            .OrderBy(b => b.Code)
            .Select(b => new BusinessProcessDto(
                b.Id, b.TenantId, b.Code, b.Name, b.Category, b.CriticalityLevel,
                b.OwnerId, b.OrgUnitId, b.MaxTolerableDowntimeMinutes,
                b.RecoveryTimeObjectiveMinutes, b.DependsOnSystemIds))
            .ToListAsync(ct);

        return Results.Ok(list);
    }
}
