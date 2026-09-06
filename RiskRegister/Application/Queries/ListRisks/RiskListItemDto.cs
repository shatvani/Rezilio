namespace Rezilio.Modules.RiskRegister.Application.Queries.ListRisks;

public sealed record RiskListItemDto(
    Guid Id,
    string Code,
    RiskDomain Domain,
    string Title,
    Guid? OwnerId,
    RiskStatus Status);
