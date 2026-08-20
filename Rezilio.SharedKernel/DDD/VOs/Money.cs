using Rezilio.SharedKernel.Results;

namespace Rezilio.SharedKernel.DDD.VOs;

public record Money
{
    public decimal Osszeg { get; init; }
    public string Penznem { get; init; } = "HUF";

    private Money() { }

    public static Result<Money> Create(decimal osszeg, string penznem = "HUF")
    {
        var errors = new List<string>();

        if (osszeg < 0)
        {
            errors.Add("Az összeg nem lehet negatív.");
        }

        if (string.IsNullOrWhiteSpace(penznem))
        {
            errors.Add("A pénznem kötelező.");
        }

        return errors.Count > 0
            ? Result.Failure<Money>(errors)
            : Result.Success(new Money { Osszeg = osszeg, Penznem = penznem });
    }

    public Money Kedvezmenynel(decimal szazalek) =>
        new() { Osszeg = Math.Round(Osszeg * (1 - szazalek / 100), 2), Penznem = Penznem };

    /// <summary>
    /// Árindexeléssel módosított összeg.
    /// arindex = 100 → nincs változás, arindex = 105 → 5%-os emelés.
    /// Ft-nál egész számra kerekítünk (0 tizedesjegy).
    /// </summary>
    public Money ApplyIndex(decimal arindex) =>
        new() { Osszeg = Math.Round(Osszeg * arindex / 100, 0), Penznem = Penznem };
}
