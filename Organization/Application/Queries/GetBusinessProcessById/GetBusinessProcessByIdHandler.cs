using Rezilio.SharedKernel.Results;

namespace Rezilio.Modules.Organization.Application.Queries.GetBusinessProcessById;

public sealed class GetBusinessProcessByIdHandler
{
    private readonly OrganizationDbContext _db;

    public GetBusinessProcessByIdHandler(OrganizationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BusinessProcessDto>> Handle(GetBusinessProcessByIdQuery query, CancellationToken ct)
    {
        BusinessProcessDto? dto = await _db.BusinessProcesses
            .Where(b => b.Id == query.Id && b.TenantId == query.TenantId)
            .Select(b => new BusinessProcessDto(
                b.Id, b.TenantId, b.Code, b.Name, b.Category, b.CriticalityLevel,
                b.OwnerId, b.OrgUnitId, b.MaxTolerableDowntimeMinutes,
                b.RecoveryTimeObjectiveMinutes, b.DependsOnSystemIds))
            .FirstOrDefaultAsync(ct);

        if (dto is null)
        {
            return Result.Failure<BusinessProcessDto>("Business process not found.");
        }

        return Result.Success(dto);
    }
}
