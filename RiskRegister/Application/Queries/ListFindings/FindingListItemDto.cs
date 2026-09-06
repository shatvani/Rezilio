namespace Rezilio.Modules.RiskRegister.Application.Queries.ListFindings;

public sealed record FindingListItemDto(
    Guid Id,
    FindingSource Source,
    string Description,
    FindingSeverity Severity,
    FindingStatus Status,
    Guid? OwnerId);
