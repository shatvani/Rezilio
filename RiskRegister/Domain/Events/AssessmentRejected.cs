using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// Modulon belüli event — az AssessmentRejectedHandler ebből dönti el (a Risk korábbi
/// Assessment-történetének ismeretében), hogy a Risk.Status Draft-ba vagy Active-ba esik
/// vissza (ld. docs/design/risk-register-design.md §3.3).
/// </summary>
public sealed record AssessmentRejected(Guid AssessmentId, Guid TenantId, Guid RiskId, string RejectionReason) : DomainEvent;
