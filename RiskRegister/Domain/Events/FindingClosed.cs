using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record FindingClosed(Guid FindingId, Guid TenantId) : DomainEvent;
