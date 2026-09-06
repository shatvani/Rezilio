using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

public sealed record FindingCreated(Guid FindingId, Guid TenantId, FindingSource Source, FindingSeverity Severity) : DomainEvent;
