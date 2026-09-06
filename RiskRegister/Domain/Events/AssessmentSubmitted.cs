using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// Modulon belüli event — az AssessmentSubmittedHandler ebből váltja ki a
/// Risk.Status → UnderReview átmenetet (ld. docs/design/risk-register-design.md §3.3).
/// Nem lép át modulhatáron.
/// </summary>
public sealed record AssessmentSubmitted(Guid AssessmentId, Guid TenantId, Guid RiskId) : DomainEvent;
