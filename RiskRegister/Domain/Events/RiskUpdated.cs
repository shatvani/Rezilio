using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record RiskUpdated(Guid RiskId, Guid TenantId) : DomainEvent;
