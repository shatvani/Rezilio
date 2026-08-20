namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Marker interfész — jelzi, hogy egy osztály DDD-s entitás.
///
/// Miért létezik ez az üres interfész?
/// - Generikus kényszerekhez: where T : IEntity
/// - EF Core interceptoroknál, middleware-eknél típus szerint szűrhetünk,
///   anélkül, hogy ismernénk az Id típusát (int, Guid, stb.)
/// - Az IEntity és IEntity szétválasztása lehetővé teszi, hogy
///   Id-típus nélkül is hivatkozhassunk entitásokra.
/// </summary>
public interface IEntity { }

/// <summary>
/// Entitás interfész erősen típusos azonosítóval.
///
/// Miért csak getter van a setter nélkül?
/// - Az Id kívülről nem állítható be — csak az entitás saját maga
///   (protected set az Entity-ben) vagy az ORM (EF Core reflection) írhatja.
/// - Ha az interfész settet is tartalmazna, bármely kód módosíthatná,
///   ami megtörné az entitás identitás-invariánsát.
///
/// TId: az azonosító típusa — pl. int (lookup táblák), Guid (aggregate rootok)
/// </summary>
public interface IEntity<T> : IEntity
{
    T Id { get; }
}
