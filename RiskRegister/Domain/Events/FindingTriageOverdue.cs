using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// Open állapotban, triázs nélkül eltöltött idő túllépése (3 munkanap, ld. §3.2).
/// Mivel ilyenkor még nincs OwnerId, a jelzés szerepkör-alapú (minden RiskManager-nek).
/// A kiváltó mechanizmus (időzített háttérfolyamat) egyelőre nincs bekötve — ld.
/// docs/design/risk-register-design.md §3.2 és a Finding-fázis lezáró review-ja.
/// </summary>
public sealed record FindingTriageOverdue(Guid FindingId, Guid TenantId) : DomainEvent;
