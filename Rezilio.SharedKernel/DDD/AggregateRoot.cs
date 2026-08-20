namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Alap Aggregate Root — domain event kezeléssel, audit nélkül.
///
/// Mikor használd ezt (és nem AuditableAggregateRoot-ot)?
/// - Lookup/törzsadat táblákhoz: főtevékenységcsoport, altevékenységcsoport,
///   mértékegység, együtthatók. Ezek adminisztrátori konfigurációk, nem igényelnek
///   per-rekord audit nyomvonalat.
///
/// A domain event minta:
/// - Az AR a saját domain metódusaiban hív RaiseDomainEvent()-et, ha valami
///   üzletileg fontos dolog történt (pl. kérelem befogadva, határozat kiadva).
/// - Az eventeket NEM küldjük ki rögtön — a _domainEvents listában gyűlnek,
///   amíg az EF Core SaveChanges le nem fut.
/// - Wolverine EF Core interceptora SaveChanges után hívja ClearDomainEvents()-t,
///   kiveszi az eventeket, és outbox-on keresztül kiküldi őket.
///   Ez garantálja, hogy az event csak akkor megy ki, ha az adatbázis-írás sikeres volt.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregate<TId>
{
    // Private: kívülről nem elérhető, csak a protected RaiseDomainEvent()-en keresztül
    // bővíthető. Ez megakadályozza, hogy valaki kívülről eventeket injektáljon az AR-be.
    private readonly List<IDomainEvent> _domainEvents = new();

    // AsReadOnly(): a lista referenciája nem adható ki, csak olvasható nézetként.
    // Így a külső kód látja az eventeket, de nem tud hozzáadni/törölni.
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Domain event rögzítése az AR-ben.
    /// Protected: csak az AR saját domain metódusai hívhatják (pl. Befogad(), Elutasit()).
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);

    /// <summary>
    /// Visszaadja az eventeket tömbként, majd törli a belső listát.
    ///
    /// Miért tömb és nem lista?
    /// - A Wolverine/MediatR infrastruktúra tömböt vár.
    /// - A ToArray() pillanatfelvételt készít — a Clear() után is megmaradnak
    ///   az eventek a visszaadott tömbben (a lista törlése nem érinti a tömböt).
    /// </summary>
    public IDomainEvent[] ClearDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}

/// <summary>
/// Auditálható Aggregate Root — üzleti folyamatokhoz, ahol fontos tudni
/// ki és mikor hozta létre/módosította az adatot.
///
/// Mikor használd ezt?
/// - RentalRequest (közterület-használati kérelem)
/// - LeaseAgreement (megállapodás)
/// - Decision (határozat)
/// - Bármely AR, ahol a "ki csinálta?" üzleti kérdés, nem csak technikai log.
///
/// Miért DateTimeOffset és nem DateTime?
/// - A DateTime nem tárol időzóna-információt — UTC-t és local time-ot összekeverhetsz.
/// - A DateTimeOffset mindig tartalmazza az eltolást (offset), így egyértelmű,
///   hogy melyik pillanatról van szó, függetlenül a szerver időzónájától.
///   Ez kritikus elosztott/cloud környezetben (Azure, Docker).
///
/// Miért nullable a LastModified és LastModifiedBy?
/// - Létrehozáskor még nem volt módosítás — null = "még soha nem módosították".
///   Ez szemantikailag helyes, ellentétben egy üres string-gel vagy MinValue-val.
///
/// Miért nullable a CorrelationId?
/// - Nem minden művelet érkezik HTTP kérésen keresztül (pl. háttérfolyamat,
///   scheduled job). Ilyenkor nincs correlation ID — null jelzi ezt.
/// </summary>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Módosítás auditálása — a handler hívja SaveChanges előtt.
    /// Public: a handler (nem az AR) tudja, melyik user végzi a műveletet.
    /// </summary>
    public void AuditChanges(string user)
    {
        LastModified = DateTimeOffset.UtcNow;
        LastModifiedBy = user;
    }
}
