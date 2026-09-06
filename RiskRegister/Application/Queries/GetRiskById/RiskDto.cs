namespace Rezilio.Modules.RiskRegister.Application.Queries.GetRiskById;

public sealed record RiskDto(
    Guid Id,
    Guid TenantId,
    string Code,
    RiskDomain Domain,
    string Title,
    string Description,
    Guid? OwnerId,
    RiskStatus Status,
    Guid? CopiedFromRiskId,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? LastModified,
    string? LastModifiedBy);
