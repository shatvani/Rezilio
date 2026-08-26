using Organization.Application.Queries.GetBusinessProcessById;
using Rezilio.Modules.Organization.Infrastructure;
using Rezilio.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Organization.Application.Queries.GetBusinessProcessesByTenant;

public sealed class GetBusinessProcessesByTenantHandler
{
    private readonly OrganizationDbContext _db;

    public GetBusinessProcessesByTenantHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<BusinessProcessDto>>> Handle(GetBusinessProcessesByTenantQuery query, CancellationToken ct)
    {
        List<BusinessProcessDto> list = await _db.BusinessProcesses
            .Where(b => b.TenantId == query.TenantId)
            .OrderBy(b => b.Code)
            .Select(b => new BusinessProcessDto(
                b.Id, b.TenantId, b.Code, b.Name, b.Category, b.CriticalityLevel,
                b.OwnerId, b.OrgUnitId, b.MaxTolerableDowntimeMinutes,
                b.RecoveryTimeObjectiveMinutes, b.DependsOnSystemIds))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}
