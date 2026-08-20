namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Alap entitás osztály — minden DDD entitás ebből örököl.
///
/// Mi különbözteti meg az entitást a Value Objecttől?
/// - Az entitásnak van egyedi azonosítója (Id), és az egyenlőség az Id alapján dől el.
///   Két entitás akkor ugyanaz, ha ugyanaz az Id-jük — a tartalmuk lehet különböző.
/// - A Value Objectnek nincs Id-je, az egyenlőség a tartalom alapján dől el.
///   Példa: a "10 000 Ft" == "10 000 Ft", Id nélkül.
///
/// Miért public init az Id-n?
/// - Az init accessor csak objektum létrehozásakor (object initializer vagy konstruktor)
///   engedi meg az Id beállítását — futásidőben nem írható felül.
/// - Ez szükséges az EF Core HasData() seed adatokhoz, ahol new PortFactor { Id = 1 }
///   szintaxist használunk.
/// - EF Core reflection-nel is képes feltölteni betöltéskor.
///
/// Miért default! az inicializáló érték?
/// - A T típus lehet int (értéktípus) vagy Guid, string (referencia típus).
///   A default! azt mondja a fordítónak: "tudom, hogy null lehet, de én kezelem" —
///   EF Core mindig beállítja az Id-t betöltéskor, a Create() factory pedig
///   explicit értéket ad neki (vagy az AR esetén az adatbázis generálja).
/// </summary>
public abstract class Entity<T> : IEntity<T>
{
    public T Id { get; init; } = default!;
}
