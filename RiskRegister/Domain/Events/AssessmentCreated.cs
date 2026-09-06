using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record AssessmentCreated(Guid AssessmentId, Guid TenantId, Guid RiskId) : DomainEvent;
