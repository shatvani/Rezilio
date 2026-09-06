using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// A RiskCreated a modulhatáron átnyúló, külső eseményt is kiváltja (Monitoring,
/// Reporting felé, ld. docs/design/risk-register-design.md §2.2/§2.5).
/// </summary>
public sealed record RiskCreated(Guid RiskId, Guid TenantId, string Code, RiskDomain Domain) : DomainEvent;
