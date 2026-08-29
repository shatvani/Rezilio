namespace Rezilio.Modules.Organization.Application.Commands.UpdateBusinessProcess;

public sealed record UpdateBusinessProcessCommand(
    Guid Id,
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
