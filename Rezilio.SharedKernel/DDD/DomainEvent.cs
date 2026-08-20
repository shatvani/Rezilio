namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Alap domain event implementáció — ebből örököl minden konkrét event.
///
/// Miért abstract record és nem abstract class?
/// - A record értékszemantikával rendelkezik: az egyenlőség a tartalom alapján dől el,
///   nem a referencia alapján. Ez domain eventeknél helyes — két "KerelemBefogadva"
///   event azonos adatokkal logikailag azonos eseményt jelent.
/// - A record automatikusan generál ToString()-et, ami hasznos logoláshoz:
///   KerelemBefogadva { EventId = ..., OccurredOn = ..., KerelemiId = 42 }
/// - Az öröklés (record : record) megőrzi ezeket az előnyöket a konkrét eventeknél is.
///
/// Miért csak getter van (nincs init)?
/// - Az EventId és OccurredOn az event létrehozásakor rögzül, és soha nem változik.
///   Az init setter lehetővé tenné az értékek felülírását — ezt nem akarjuk.
///   A csak-getter property-k record-ban inicializálhatók a deklarációban (= Guid.NewGuid()),
///   ami pontosan ezt a "egyszer beállítódik, aztán immutable" szemantikát adja.
///
/// EventType implementáció:
/// - GetType().FullName! visszaadja a teljes típusnevet: "FeeBoard.Domain.Events.MainActivityGroupCreated"
///   Ez egyedi minden event-típusra, és Wolverine routing-ban használható.
/// - Az absztrakt DomainEvent-ben implementált, így nem kell minden konkrét event-ben megírni.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    // Guid.NewGuid() minden új event példányhoz egyedi ID-t generál.
    public Guid EventId { get; } = Guid.NewGuid();

    // UtcNow: az event pillanata timezone-biztos formában.
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    // A konkrét típus teljes neve — pl. "FeeBoard.Domain.Events.MainActivityGroupCreated"
    public string EventType => GetType().FullName!;
}
