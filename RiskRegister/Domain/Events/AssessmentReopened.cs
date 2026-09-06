using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// A ReopenAssessment Command váltja ki (ld. docs/design/risk-register-design.md §2.2/§3.3,
/// 2026-09-06-i döntés) — a Rejected → Draft átdolgozás-nyitás explicit, auditálható
/// jelzése. Nincs Risk-oldali hatása (a Risk.Status ekkor már Active, a korábbi
/// AssessmentRejectedHandler futása óta).
/// </summary>
public sealed record AssessmentReopened(Guid AssessmentId, Guid TenantId, Guid RiskId) : DomainEvent;
