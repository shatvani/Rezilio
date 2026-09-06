namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRiskFromArchivedRisk;

public sealed record CreateRiskFromArchivedRiskCommand(RiskDomain? Domain, string? Title, string? Description);
