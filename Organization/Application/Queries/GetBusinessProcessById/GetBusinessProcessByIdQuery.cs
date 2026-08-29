namespace Rezilio.Modules.Organization.Application.Queries.GetBusinessProcessById;

public sealed record GetBusinessProcessByIdQuery(Guid Id, Guid TenantId);

public sealed record BusinessProcessDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string Category,
    CriticalityLevel CriticalityLevel,
    Guid? OwnerId,
    Guid? OrgUnitId,
    int? MaxTolerableDowntimeMinutes,
    int? RecoveryTimeObjectiveMinutes,
    List<Guid> DependsOnSystemIds);
