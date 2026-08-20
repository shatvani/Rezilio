using Rezilio.SharedKernel.Results;

namespace Rezilio.SharedKernel.DDD.VOs;

public record DateRange
{
    public DateTime Kezd { get; init; }
    public DateTime? Vege { get; init; }

    private DateRange() { } // EF Core-nak kell

    public static Result<DateRange> Create(DateTime kezd, DateTime? vege)
    {
        var errors = new List<string>();

        if (kezd == default)
        {
            errors.Add("A kezdő dátum kötelező.");
        }

        if (vege.HasValue && vege.Value < kezd)
        {
            errors.Add("A végdátum nem lehet korábbi, mint a kezdő dátum.");
        }

        return errors.Count > 0
            ? Result.Failure<DateRange>(errors)
            : Result.Success(new DateRange { Kezd = kezd, Vege = vege });
    }

    public bool IsAktiv => Kezd <= DateTime.UtcNow;

    public bool Atfed(DateRange masik) =>
        Kezd < (masik.Vege ?? DateTime.MaxValue) &&
        (Vege ?? DateTime.MaxValue) > masik.Kezd;
}
