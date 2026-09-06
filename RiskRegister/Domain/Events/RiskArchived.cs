using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record RiskArchived(Guid RiskId, Guid TenantId) : DomainEvent;
