using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record FindingLinkedToRisk(Guid FindingId, Guid TenantId, Guid RiskId) : DomainEvent;
