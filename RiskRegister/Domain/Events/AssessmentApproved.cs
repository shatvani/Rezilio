using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// Modulon belüli event — az AssessmentApprovedHandler ebből váltja ki a
/// Risk.Status → Active átmenetet. Emellett ez az egyetlen Assessment-esemény, amely a
/// modulhatáron is átlép: a handler ebből generálja a Monitoring/Reporting felé szóló
/// RiskScoreChanged cross-module eseményt (ld. §2.2/§3.3).
/// </summary>
public sealed record AssessmentApproved(Guid AssessmentId, Guid TenantId, Guid RiskId, int ResidualScore) : DomainEvent;
