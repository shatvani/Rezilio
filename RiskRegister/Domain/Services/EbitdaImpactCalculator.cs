using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.RiskRegister.Domain.Services;

/// <summary>
/// Stateless domain service: az Assessment becsült pénzügyi hatását (EstimatedFinancialImpact)
/// és a feloldott EBITDA-alapértéket (ld. EbitdaBaselineLookupQuery, Organization modul)
/// viszonyítja egymáshoz százalékban. Ld. docs/design/risk-register-design.md §2.4.
///
/// Null baseline esetén a visszatérési érték null — ez NEM hibaállapot, csupán azt jelenti,
/// hogy sem a kontextus-szintű (OrgUnit/BusinessProcess), sem a tenant-szintű alapérték
/// nincs beállítva, tehát az EBITDA-arányos hatás nem számítható.
/// </summary>
public static class EbitdaImpactCalculator
{
    /// <summary>
    /// Kiszámítja az EstimatedFinancialImpact EBITDA-alapértékhez viszonyított arányát
    /// százalékban. <paramref name="estimatedFinancialImpact"/> null esetén is null-t ad
    /// vissza (nincs mit viszonyítani). A két Money-nak azonos pénznemben kell lennie —
    /// eltérő pénznem esetén <see cref="InvalidOperationException"/>-t dob (ld. Money.EnsureSameCurrency).
    /// </summary>
    public static decimal? Calculate(Money? estimatedFinancialImpact, Money? ebitdaBaseline)
    {
        if (estimatedFinancialImpact is null || ebitdaBaseline is null)
        {
            return null;
        }

        if (estimatedFinancialImpact.Currency != ebitdaBaseline.Currency)
        {
            throw new InvalidOperationException(
                $"Az EstimatedFinancialImpact ({estimatedFinancialImpact.Currency}) és az EBITDA-alapérték " +
                $"({ebitdaBaseline.Currency}) pénzneme eltér — az EBITDA-hatás nem számítható ki eltérő pénznemek között.");
        }

        if (ebitdaBaseline.Amount == 0m)
        {
            // Nulla EBITDA-alapérték mellett az arány matematikailag nem értelmezhető
            // (osztás nullával) — ez is null-t ad vissza, nem kivételt dob, mert ez
            // valós, előforduló üzleti állapot (pl. veszteséges tenant).
            return null;
        }

        return Math.Round(estimatedFinancialImpact.Amount / ebitdaBaseline.Amount * 100m, 2);
    }
}
