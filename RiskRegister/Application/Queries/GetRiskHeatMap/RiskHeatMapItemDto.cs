namespace Rezilio.Modules.RiskRegister.Application.Queries.GetRiskHeatMap;

public sealed record RiskHeatMapItemDto(
    Guid RiskId,
    string Code,
    RiskDomain Domain,
    string Title,
    RiskStatus Status,
    int ResidualLikelihood,
    int ResidualImpact,
    int ResidualScore);
