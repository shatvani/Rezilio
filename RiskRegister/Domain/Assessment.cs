using Rezilio.Modules.RiskRegister.Domain.Events;
using Rezilio.Modules.RiskRegister.Domain.Services;
using Rezilio.SharedKernel.DDD;
using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// Egy adott Risk egy időpillanatban végzett kockázatértékelése — Inherens/Reziduális/
/// Cél Likelihood-Impact-Score hármasokkal, opcionális pénzügyi hatásbecsléssel és
/// EBITDA-arányos kontextussal, valamint egy külön jóváhagyási munkafolyamattal
/// (ApprovalStatus). Ld. docs/design/risk-register-design.md §2.2.
///
/// A Score mezők KIZÁRÓLAG a RiskScoreCalculator-on, az EbitdaImpactPercentage/
/// EbitdaBaselineSnapshot pedig KIZÁRÓLAG az EbitdaImpactCalculator-on keresztül
/// állítható be — ez aggregátum-invariáns, ezért mindkét számítás a Create/Update
/// metódusokon belül, nem az Application-rétegben történik.
///
/// A RiskId nem-Archived ellenőrzése, az "egy Risk-hez egyszerre csak egy Draft/Submitted
/// Assessment" szabály, és az EbitdaBaselineLookupQuery meghívása Application-rétegbeli
/// felelősség (cross-aggregate/cross-module lekérdezést igényelnek) — nem ez az aggregátum
/// ellenőrzi őket.
/// </summary>
public sealed class Assessment : AuditableAggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid RiskId { get; private set; }

    public int InherentLikelihood { get; private set; }
    public int InherentImpact { get; private set; }
    public int InherentScore { get; private set; }

    public int ResidualLikelihood { get; private set; }
    public int ResidualImpact { get; private set; }
    public int ResidualScore { get; private set; }

    public int? TargetLikelihood { get; private set; }
    public int? TargetImpact { get; private set; }
    public int? TargetScore { get; private set; }

    public Money? EstimatedFinancialImpact { get; private set; }

    /// <summary>Számított-only — ld. osztály-szintű megjegyzés.</summary>
    public decimal? EbitdaImpactPercentage { get; private set; }

    /// <summary>
    /// A ténylegesen alkalmazott EBITDA-alapérték audit-célú lefagyasztása a számítás
    /// pillanatában (ld. §2.3) — utólagos TenantSettings/OrgUnit-módosítás nem írja felül.
    /// </summary>
    public Money? EbitdaBaselineSnapshot { get; private set; }

    /// <summary>Kölcsönösen kizárja a BusinessProcessId-t (ld. §2.3 aggregátum-invariáns).</summary>
    public Guid? ImpactContextOrgUnitId { get; private set; }

    /// <summary>Kölcsönösen kizárja az OrgUnitId-t (ld. §2.3 aggregátum-invariáns).</summary>
    public Guid? ImpactContextBusinessProcessId { get; private set; }

    /// <summary>A KeyPerson, aki az értékelést végezte — létezés/aktív-ellenőrzés Application-rétegbeli felelősség.</summary>
    public Guid AssessedBy { get; private set; }

    public ApprovalStatus ApprovalStatus { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private Assessment() { }

    public static Assessment Create(
        Guid tenantId,
        Guid riskId,
        int inherentLikelihood,
        int inherentImpact,
        int residualLikelihood,
        int residualImpact,
        int? targetLikelihood,
        int? targetImpact,
        Money? estimatedFinancialImpact,
        Money? ebitdaBaselineSnapshot,
        Guid? impactContextOrgUnitId,
        Guid? impactContextBusinessProcessId,
        Guid assessedBy)
    {
        EnsureMutuallyExclusiveImpactContext(impactContextOrgUnitId, impactContextBusinessProcessId);

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RiskId = riskId,
            ImpactContextOrgUnitId = impactContextOrgUnitId,
            ImpactContextBusinessProcessId = impactContextBusinessProcessId,
            AssessedBy = assessedBy,
            ApprovalStatus = ApprovalStatus.Draft
        };

        assessment.SetScoresAndEbitda(
            inherentLikelihood, inherentImpact,
            residualLikelihood, residualImpact,
            targetLikelihood, targetImpact,
            estimatedFinancialImpact, ebitdaBaselineSnapshot);

        assessment.RaiseDomainEvent(new AssessmentCreated(assessment.Id, tenantId, riskId));
        return assessment;
    }

    /// <summary>
    /// Csak Draft állapotban hívható (ld. §3.3 — a Rejected → Draft átdolgozás-nyitás
    /// külön ReopenAssessment Commanddal/Reopen() metódussal történik, 2026-09-06-i döntés,
    /// nem ez a metódus feladata).
    /// </summary>
    public void Update(
        int inherentLikelihood,
        int inherentImpact,
        int residualLikelihood,
        int residualImpact,
        int? targetLikelihood,
        int? targetImpact,
        Money? estimatedFinancialImpact,
        Money? ebitdaBaselineSnapshot,
        Guid? impactContextOrgUnitId,
        Guid? impactContextBusinessProcessId)
    {
        if (ApprovalStatus != ApprovalStatus.Draft)
        {
            throw new InvalidOperationException("Az Assessment csak Draft állapotban módosítható.");
        }

        EnsureMutuallyExclusiveImpactContext(impactContextOrgUnitId, impactContextBusinessProcessId);

        ImpactContextOrgUnitId = impactContextOrgUnitId;
        ImpactContextBusinessProcessId = impactContextBusinessProcessId;

        SetScoresAndEbitda(
            inherentLikelihood, inherentImpact,
            residualLikelihood, residualImpact,
            targetLikelihood, targetImpact,
            estimatedFinancialImpact, ebitdaBaselineSnapshot);

        // Nincs AssessmentUpdated esemény — a §2.2 lezárt domain-event lista nem
        // tartalmaz ilyet (csak Created/Submitted/Approved/Rejected/Reopened), mert az
        // Update-nek nincs cross-aggregate következménye.
    }

    /// <summary>
    /// Draft → Submitted. Ez fagyasztja be az adatokat (Update ezután már nem hívható).
    /// Modulon belüli event-handler (nem közvetlen hívás) végzi a Risk.Status →
    /// UnderReview átmenetet, ld. §3.3.
    /// </summary>
    public void Submit()
    {
        if (ApprovalStatus != ApprovalStatus.Draft)
        {
            throw new InvalidOperationException("Csak Draft állapotú Assessment nyújtható be.");
        }

        ApprovalStatus = ApprovalStatus.Submitted;
        RaiseDomainEvent(new AssessmentSubmitted(Id, TenantId, RiskId));
    }

    /// <summary>
    /// Submitted → Approved. Kiváltja a modulon belüli Risk.Status → Active átmenetet
    /// és a modulhatáron átnyúló RiskScoreChanged eseményt (Monitoring/Reporting) — ld.
    /// §2.2/§3.3 üzleti szabály #1 (Approve mindig Active-ba viszi a Risk-et, függetlenül
    /// a kockázat-étvágy kimenetelétől).
    /// </summary>
    public void Approve(Guid approvedBy)
    {
        if (ApprovalStatus != ApprovalStatus.Submitted)
        {
            throw new InvalidOperationException("Csak Submitted állapotú Assessment hagyható jóvá.");
        }

        ApprovalStatus = ApprovalStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new AssessmentApproved(Id, TenantId, RiskId, ResidualScore));
    }

    /// <summary>
    /// Submitted → Rejected. A Risk.Status visszaesését (Draft vagy Active, az adott
    /// Risk Assessment-történetétől függően) a modulon belüli AssessmentRejectedHandler
    /// dönti el — ld. §3.3.
    /// </summary>
    public void Reject(string rejectionReason)
    {
        if (ApprovalStatus != ApprovalStatus.Submitted)
        {
            throw new InvalidOperationException("Csak Submitted állapotú Assessment utasítható el.");
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("A RejectionReason kötelező elutasításkor.", nameof(rejectionReason));
        }

        ApprovalStatus = ApprovalStatus.Rejected;
        RejectionReason = rejectionReason;
        RaiseDomainEvent(new AssessmentRejected(Id, TenantId, RiskId, rejectionReason));
    }

    /// <summary>
    /// Rejected → Draft (átdolgozás megkezdése) — külön, dedikált lépés (2026-09-06-i
    /// döntés, ld. §2.2/§3.3), elkülönítve az Update egyszerű szerkesztésétől. Nem törli
    /// a korábbi RejectionReason-t (audit-nyom — az Update majd felülírja, ha a felhasználó
    /// ténylegesen módosítja az adatokat).
    /// </summary>
    public void Reopen()
    {
        if (ApprovalStatus != ApprovalStatus.Rejected)
        {
            throw new InvalidOperationException("Csak Rejected állapotú Assessment nyitható újra.");
        }

        ApprovalStatus = ApprovalStatus.Draft;
        RaiseDomainEvent(new AssessmentReopened(Id, TenantId, RiskId));
    }

    private void SetScoresAndEbitda(
        int inherentLikelihood,
        int inherentImpact,
        int residualLikelihood,
        int residualImpact,
        int? targetLikelihood,
        int? targetImpact,
        Money? estimatedFinancialImpact,
        Money? ebitdaBaselineSnapshot)
    {
        InherentLikelihood = inherentLikelihood;
        InherentImpact = inherentImpact;
        InherentScore = RiskScoreCalculator.CalculateScore(inherentLikelihood, inherentImpact);

        ResidualLikelihood = residualLikelihood;
        ResidualImpact = residualImpact;
        ResidualScore = RiskScoreCalculator.CalculateScore(residualLikelihood, residualImpact);

        // TargetScore csak akkor számítható, ha mindkét Target mező ki van töltve —
        // a kettő "együtt vagy sehogy" (ld. §2.2, opcionális céltűzés).
        if (targetLikelihood is { } tl && targetImpact is { } ti)
        {
            TargetLikelihood = tl;
            TargetImpact = ti;
            TargetScore = RiskScoreCalculator.CalculateScore(tl, ti);
        }
        else
        {
            TargetLikelihood = null;
            TargetImpact = null;
            TargetScore = null;
        }

        EstimatedFinancialImpact = estimatedFinancialImpact;
        EbitdaBaselineSnapshot = ebitdaBaselineSnapshot;
        EbitdaImpactPercentage = EbitdaImpactCalculator.Calculate(estimatedFinancialImpact, ebitdaBaselineSnapshot);
    }

    private static void EnsureMutuallyExclusiveImpactContext(Guid? orgUnitId, Guid? businessProcessId)
    {
        if (orgUnitId is not null && businessProcessId is not null)
        {
            throw new InvalidOperationException(
                "Az ImpactContextOrgUnitId és ImpactContextBusinessProcessId kölcsönösen kizárják egymást — legfeljebb az egyik tölthető ki.");
        }
    }
}
