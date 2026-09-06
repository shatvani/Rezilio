namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateRisk;

public sealed record CreateRiskCommand(
    Guid TenantId,
    RiskDomain Domain,
    string Title,
    string Description,
    Guid? OwnerId = null);
