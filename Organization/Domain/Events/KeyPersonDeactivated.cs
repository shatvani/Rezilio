using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain.Events;

/// <summary>
/// A KeyPerson deaktiválva lett (soft-delete). Más modulok (pl. RiskRegister) a jövőben
/// feliratkozhatnak erre, hogy pl. figyelmeztessenek a még hozzá rendelt Risk/Control/Action
/// rekordokra — ez egyelőre (2026-09-06) nincs megvalósítva, dokumentált v1-korlátozás
/// (lásd docs/design/risk-register-design.md §2.5).
/// </summary>
public sealed record KeyPersonDeactivated(Guid KeyPersonId, Guid TenantId) : DomainEvent;
