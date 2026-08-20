namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Domain event szerződése.
///
/// Mi a domain event?
/// - Egy üzleti szempontból fontos dolog, ami már megtörtént az adott AR-ben.
///   A névadás múlt időben van: KerelemBefogadva, HatarozatKiadva — nem parancs, hanem tény.
/// - A domain eventek lehetővé teszik, hogy a modulok lazán csatoltan kommunikáljanak:
///   a FeeBoard modul nem hív közvetlenül a Notifications modulba, hanem kibocsát egy
///   eventet, amelyre a Notifications feliratkozik.
///
/// A három kötelező property miért van itt?
/// - EventId (Guid): egyedi azonosító az eventhez. Idempotency-hoz kell — ha egy event
///   kétszer kerül feldolgozásra (pl. hálózati hiba után újraküldés), az EventId alapján
///   kiszűrhető a duplikáció.
/// - OccurredOn (DateTimeOffset): mikor történt az esemény, nem mikor lett feldolgozva.
///   Fontos: az event létrehozásakor rögzül (az AR-ben), nem a handler futásakor.
///   DateTimeOffset: timezone-biztos (ld. IAuditableEntity magyarázata).
/// - EventType (string): az event típusa szövegesen. Szükséges az outbox táblában való
///   tároláshoz és a Wolverine routing-jához, ahol a típusnév alapján irányítódnak az eventek.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
}
