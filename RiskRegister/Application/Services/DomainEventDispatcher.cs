using Rezilio.SharedKernel.DDD;
using Wolverine;

namespace Rezilio.Modules.RiskRegister.Application.Services;

/// <summary>
/// Segédosztály a domain-event dispatch mechanizmushoz (ld. docs/design/risk-register-design.md
/// §2.5, 2026-09-06-i döntés — az Assessment-fázison bevezetett, első ilyen mechanizmus a
/// kódbázisban): a Command handler a SaveChangesAsync UTÁN hívja ezt — kiveszi az
/// aggregátumon felgyűlt eventeket (ClearDomainEvents) és egyenként kiküldi őket a Wolverine
/// IMessageBus.PublishAsync-en keresztül, hogy a dedikált, modulon belüli event-handlerek
/// (pl. AssessmentSubmittedHandler) feldolgozhassák őket.
///
/// FONTOS, dokumentált v1-korlátozás: ez NEM ad transactional outbox-szintű garanciát — ha a
/// folyamat pontosan a SaveChangesAsync és ezen dispatch-hívás között omlik össze, az esemény
/// elveszik (nincs WolverineFx.EntityFrameworkCore csomag / EnrollDbContextInOutbox bekötve a
/// kódbázisban). Ez tudatosan elfogadott kockázat erre a fázisra — a teljes atomicitás
/// (outbox pattern) egy jövőbeli, külön megtervezendő fázis.
/// </summary>
public static class DomainEventDispatcher
{
    public static async Task DispatchAsync(AggregateRoot<Guid> aggregate, IMessageBus messageBus)
    {
        foreach (var domainEvent in aggregate.ClearDomainEvents())
        {
            await messageBus.PublishAsync(domainEvent);
        }
    }
}
