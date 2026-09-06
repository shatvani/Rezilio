using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record RiskStatusChanged(Guid RiskId, Guid TenantId, RiskStatus OldStatus, RiskStatus NewStatus) : DomainEvent;
