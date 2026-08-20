namespace Rezilio.SharedKernel.DDD;

/// <summary>
/// Value Object alap osztály — tartalom alapján egyenlő, nem referencia alapján.
///
/// Mi a Value Object (VO)?
/// - Olyan fogalom, amelynek nincs egyedi azonosítója — az értéke határozza meg.
///   Példák ebben a projektben:
///   * Money(10_000, "HUF") == Money(10_000, "HUF")  → igaz, ugyanaz az érték
///   * ValidityPeriod(2025-01-01, 2025-12-31)         → egy időszak leírása
///   * MooringCoefficient(1.2m)                       → egy együttható értéke
/// - A VO-k immutablek: ha változtatni kell, új VO-t hozunk létre, nem módosítjuk a régit.
///   Ez a funkcionális programozás alapelve — mellékhatás-mentes értékek.
///
/// Miért abstract class és nem interface?
/// - Az interface nem tartalmazhat implementált logikát (Equals, GetHashCode, operátorok).
///   Az abstract class lehetővé teszi, hogy a közös egyenlőség-logika egyszer legyen megírva,
///   és minden VO automatikusan örökölje azt — a konkrét VO csak a GetEqualityComponents()-t
///   valósítja meg.
///
/// GetEqualityComponents() minta:
///   protected override IEnumerable&lt;object?&gt; GetEqualityComponents()
///   {
///       yield return Osszeg;
///       yield return Penznem;
///   }
///   A yield return-ök sorrendje számít — ha felcseréled, más hash-t kapsz.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// A VO egyenlőség-komponensei — minden konkrét VO implementálja.
    /// Listázd fel az összes mezőt, amely az egyenlőséget meghatározza.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Egyenlőség-vizsgálat tartalom alapján.
    ///
    /// obj.GetType() != GetType(): megakadályozza, hogy egy alosztály egyenlőnek
    /// számítson az ősével, még ha ugyanazok is a komponensei — ez helyes VO-szemantika.
    /// SequenceEqual: sorrendben hasonlítja össze a komponenseket.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) { return false; }
        return ((ValueObject)obj).GetEqualityComponents()
            .SequenceEqual(GetEqualityComponents());
    }

    /// <summary>
    /// Hash kód a tartalom alapján — kötelező, ha Equals-t felülírsz.
    ///
    /// HashCode.Combine: .NET beépített, jó elosztású hash kombináló.
    /// Aggregate: végigmegy a komponenseken és összeépíti a hash-t.
    /// obj?.GetHashCode() ?? 0: null-biztos — null komponens hash-je 0.
    /// </summary>
    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
                HashCode.Combine(current, obj?.GetHashCode() ?? 0));

    /// <summary>
    /// == operátor: lehetővé teszi a természetes szintaxist (a == b) VO-knál.
    /// left?.Equals(right) ?? right is null: ha left null, akkor akkor igaz,
    /// ha right is null (null == null → true).
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>
    /// != operátor: az == tagadása — kötelező, ha == operátort definiálsz.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}

