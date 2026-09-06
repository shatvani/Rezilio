namespace Rezilio.Modules.RiskRegister.Application.Commands.UpdateAssessment;

/// <summary>
/// Ld. docs/design/risk-register-design.md §3.3 — csak Draft állapotú Assessment-en hívható
/// (a Rejected → Draft átdolgozás-nyitás a külön ReopenAssessment Command-dal történik,
/// 2026-09-06-i döntés). Ugyanazok a mezők, mint CreateAssessmentCommand-nál — az
/// AssessedBy és a RiskId szándékosan NEM módosítható itt (ld. handler).
/// </summary>
public sealed record UpdateAssessmentCommand(
    int InherentLikelihood,
    int InherentImpact,
    int ResidualLikelihood,
    int ResidualImpact,
    int? TargetLikelihood,
    int? TargetImpact,
    decimal? EstimatedFinancialImpactAmount,
    string? EstimatedFinancialImpactCurrency,
    Guid? ImpactContextOrgUnitId,
    Guid? ImpactContextBusinessProcessId);
