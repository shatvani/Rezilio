namespace Rezilio.Modules.RiskRegister.Application.Queries;

/// <summary>
/// Közös DTO a GetAssessmentById és ListAssessmentsForRisk Query-khez (mindkettő a teljes
/// Assessment-et adja vissza, csak más-más szűréssel/darabszámmal). A Money VO-kat
/// (EstimatedFinancialImpact, EbitdaBaselineSnapshot) Amount+Currency pár-mezőkre bontva adja
/// vissza — ugyanaz a konvenció, mint a TenantSettingsResult-nál (Organization modul): a DTO
/// csak primitíveket tartalmaz, nem szivárogtatja ki a VO belső szerkezetét az API-fogyasztók
/// felé.
/// </summary>
public sealed record AssessmentDto(
    Guid Id,
    Guid TenantId,
    Guid RiskId,
    int InherentLikelihood,
    int InherentImpact,
    int InherentScore,
    int ResidualLikelihood,
    int ResidualImpact,
    int ResidualScore,
    int? TargetLikelihood,
    int? TargetImpact,
    int? TargetScore,
    decimal? EstimatedFinancialImpactAmount,
    string? EstimatedFinancialImpactCurrency,
    decimal? EbitdaImpactPercentage,
    decimal? EbitdaBaselineSnapshotAmount,
    string? EbitdaBaselineSnapshotCurrency,
    Guid? ImpactContextOrgUnitId,
    Guid? ImpactContextBusinessProcessId,
    Guid AssessedBy,
    ApprovalStatus ApprovalStatus,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? LastModified,
    string? LastModifiedBy)
{
    public static AssessmentDto FromEntity(Assessment a) => new(
        a.Id, a.TenantId, a.RiskId,
        a.InherentLikelihood, a.InherentImpact, a.InherentScore,
        a.ResidualLikelihood, a.ResidualImpact, a.ResidualScore,
        a.TargetLikelihood, a.TargetImpact, a.TargetScore,
        a.EstimatedFinancialImpact?.Amount, a.EstimatedFinancialImpact?.Currency.Value,
        a.EbitdaImpactPercentage,
        a.EbitdaBaselineSnapshot?.Amount, a.EbitdaBaselineSnapshot?.Currency.Value,
        a.ImpactContextOrgUnitId, a.ImpactContextBusinessProcessId,
        a.AssessedBy, a.ApprovalStatus, a.ApprovedBy, a.ApprovedAt, a.RejectionReason,
        a.CreatedAt, a.CreatedBy, a.LastModified, a.LastModifiedBy);
}
