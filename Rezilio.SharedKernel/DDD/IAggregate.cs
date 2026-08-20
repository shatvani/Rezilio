namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Aggregate Root szerződése — a domain event kezelés kötelezettsége.
///
/// Mi az Aggregate Root (AR)?
/// - DDD-ben az AR a konzisztencia-határ: egy tranzakción belül csak egy AR-t
///   módosítunk. Az AR védi az invariánsait, és ő dönt arról, mi változhat
///   az általa "uralt" entitásokon belül.
/// - Például: RentalRequest AR uralja a csatolt dokumentumokat — a dokumentumokat
///   nem módosítod közvetlenül, hanem a RentalRequest-en keresztül.
///
/// Miért van külön IAggregate és IAggregate&lt;T&gt;?
/// - IAggregate: az event-kezelő "képesség" Id-típus nélkül leírható.
///   Olyan generikus infrastruktúra (pl. Wolverine outbox, EF interceptor) is
///   hivatkozhat rá, amely nem törődik az Id típusával.
/// - IAggregate&lt;T&gt;: kombinálja az event-kezelést (IAggregate) az azonosítóval
///   (IEntity&lt;T&gt;) — ezt használja az AggregateRoot&lt;TId&gt; tényleges implementáció.
/// </summary>
public interface IAggregate
{
    /// <summary>
    /// Az AR által kiváltott, de még ki nem küldött domain eventek.
    /// Olvasható lista — kívülről nem módosítható.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Visszaadja az eventeket tömbként, majd törli a belső listát.
    /// Wolverine hívja mentés (SaveChanges) után — kiküldi az eventeket,
    /// majd ez a metódus tisztítja a listát, hogy ne küldődjön ki kétszer.
    /// </summary>
    IDomainEvent[] ClearDomainEvents();
}

/// <summary>
/// Típusos Aggregate Root szerződés — Id-típussal kombinálva.
/// Az AggregateRoot&lt;TId&gt; ezt implementálja.
/// </summary>
public interface IAggregate<T> : IAggregate, IEntity<T> { }
