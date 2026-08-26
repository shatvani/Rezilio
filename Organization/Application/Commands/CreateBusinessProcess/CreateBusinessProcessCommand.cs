using Organization.Domain;

namespace Organization.Application.Commands.CreateBusinessProcess;

public sealed record CreateBusinessProcessCommand(
    Guid TenantId,
    string Code,
    string Name,
    string Category,
    CriticalityLevel CriticalityLevel,
    Guid? OwnerId = null,
    Guid? OrgUnitId = null,
    int? MaxTolerableDowntimeMinutes = null,
    int? RecoveryTimeObjectiveMinutes = null,
    List<Guid>? DependsOnSystemIds = null);
