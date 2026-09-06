namespace Rezilio.Modules.RiskRegister.Application.Commands.CreateAssessment;

/// <summary>
/// Ld. docs/design/risk-register-design.md §3.3. Az EstimatedFinancialImpact két külön
/// nullable mezőre bontva (Amount+Currency) — ugyanaz a minta, mint a
/// Set*AnnualEbitda command-oknál (Organization modul); a Currency-nek a tenant
/// alapértelmezett pénznemével kellene egyeznie (§2.2 invariáns), de ennek explicit
/// keresztellenőrzése v1-ben nincs bekötve — ugyanaz a dokumentált, elfogadott rés, mint a
/// SetTenantDefaultAnnualEbitda-nál.
/// </summary>
public sealed record CreateAssessmentCommand(
    Guid RiskId,
    int InherentLikelihood,
    int InherentImpact,
    int ResidualLikelihood,
    int ResidualImpact,
    int? TargetLikelihood,
    int? TargetImpact,
    decimal? EstimatedFinancialImpactAmount,
    string? EstimatedFinancialImpactCurrency,
    Guid? ImpactContextOrgUnitId,
    Guid? ImpactContextBusinessProcessId,
    Guid AssessedBy);
