namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRiskFromFinding;

public sealed record CreateRiskFromFindingCommand(RiskDomain Domain, string Title, string Description, Guid? OwnerId = null);
