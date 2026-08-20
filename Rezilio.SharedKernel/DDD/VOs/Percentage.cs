using Rezilio.SharedKernel.Results;

namespace Rezilio.SharedKernel.DDD.VOs;

public record Percentage
{
    public decimal Ertek { get; init; }

    private Percentage() { }

    public static Result<Percentage> Create(decimal ertek)
    {
        var errors = new List<string>();

        if (ertek < 0)
        {
            errors.Add("A százalék nem lehet negatív.");
        }

        if (ertek > 100)
        {
            errors.Add("A százalék nem lehet nagyobb 100-nál.");
        }

        return errors.Count > 0
            ? Result.Failure<Percentage>(errors)
            : Result.Success(new Percentage { Ertek = ertek });
    }

    public decimal Alkalmaz(decimal alaposszeg) =>
        Math.Round(alaposszeg * (1 - Ertek / 100), 2);
}
