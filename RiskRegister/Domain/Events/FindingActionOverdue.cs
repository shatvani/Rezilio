using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// Triaged állapotban, Risk/Action nélkül eltöltött idő túllépése (7 naptári nap, ld.
/// §3.2). Ilyenkor már van OwnerId, a jelzés neki szól. A kiváltó mechanizmus egyelőre
/// nincs bekötve — ld. FindingTriageOverdue doc-comment.
/// </summary>
public sealed record FindingActionOverdue(Guid FindingId, Guid TenantId, Guid OwnerId) : DomainEvent;
