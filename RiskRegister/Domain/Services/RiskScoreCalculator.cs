namespace Rezilio.Modules.RiskRegister.Domain.Services;

/// <summary>
/// A kockázati mátrix (Likelihood × Impact) sávjai — ld. docs/design/risk-register-design.md
/// §2.4. A sávhatárok véglegesített (2026-09-06-án lezárt), nem konfigurálható értékek v1-ben.
/// </summary>
public enum RiskScoreBand
{
    Low,
    Medium,
    Significant,
    Critical
}

/// <summary>
/// Stateless domain service: Likelihood × Impact (1–5 mindkettő) → Score (1–25) + minőségi sáv.
/// Ld. docs/design/risk-register-design.md §2.4. Az Assessment aggregát Inherent/Residual/
/// Target Score mezői KIZÁRÓLAG ezen a szolgáltatáson keresztül számíthatók — közvetlen
/// beállításuk tilos (aggregát-invariáns).
/// </summary>
public static class RiskScoreCalculator
{
    public const int MinLikelihoodOrImpact = 1;
    public const int MaxLikelihoodOrImpact = 5;

    public static int CalculateScore(int likelihood, int impact)
    {
        ValidateRange(likelihood, nameof(likelihood));
        ValidateRange(impact, nameof(impact));

        return likelihood * impact;
    }

    /// <summary>
    /// A sávhatárok véglegesítettek (2026-09-06): 1–5 Alacsony (Low), 6–10 Közepes (Medium),
    /// 11–19 Jelentős (Significant), 20–25 Kritikus (Critical).
    /// </summary>
    public static RiskScoreBand CalculateBand(int score)
    {
        return score switch
        {
            >= 1 and <= 5 => RiskScoreBand.Low,
            >= 6 and <= 10 => RiskScoreBand.Medium,
            >= 11 and <= 19 => RiskScoreBand.Significant,
            >= 20 and <= 25 => RiskScoreBand.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(score), score, "A Score értéknek 1 és 25 között kell lennie.")
        };
    }

    private static void ValidateRange(int value, string paramName)
    {
        if (value is < MinLikelihoodOrImpact or > MaxLikelihoodOrImpact)
        {
            throw new ArgumentOutOfRangeException(paramName, value,
                $"A Likelihood/Impact értéknek {MinLikelihoodOrImpact} és {MaxLikelihoodOrImpact} között kell lennie.");
        }
    }
}
