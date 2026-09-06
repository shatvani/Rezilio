using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain.Events;

/// <summary>
/// A Finding → ActionCreated átmenetet jelzi. Az Action aggregátum maga a
/// TreatmentPlan+Control+Action fázisban készül el — ez az esemény már most
/// definiálva van (ld. §2.2), de a CreateActionFromFinding Command, ami kiváltaná,
/// tudatosan elhalasztva addig.
/// </summary>
public sealed record FindingActionCreated(Guid FindingId, Guid TenantId, Guid ActionId) : DomainEvent;
