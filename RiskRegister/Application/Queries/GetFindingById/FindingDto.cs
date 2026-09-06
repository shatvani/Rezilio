namespace Rezilio.Modules.RiskRegister.Application.Queries.GetFindingById;

public sealed record FindingDto(
    Guid Id,
    Guid TenantId,
    FindingSource Source,
    string Description,
    FindingSeverity Severity,
    FindingStatus Status,
    Guid? LinkedRiskId,
    Guid? OwnerId,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? LastModified,
    string? LastModifiedBy);
