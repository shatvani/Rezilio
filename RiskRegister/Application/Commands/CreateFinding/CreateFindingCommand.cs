namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateFinding;

public sealed record CreateFindingCommand(FindingSource Source, string Description, FindingSeverity Severity);
