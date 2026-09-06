namespace Rezilio.Modules.RiskRegister.Application.Queries.GetOverdueFindings;

/// <summary>
/// Belső eredménytípus a monitoring-háttérfolyamat számára (ld. §3.2) — nincs hozzá
/// HTTP végpont, mert nem felhasználó-kezdeményezte lekérdezés.
/// </summary>
public sealed record OverdueFindingsResult(
    IReadOnlyList<Guid> TriageOverdueFindingIds,
    IReadOnlyList<(Guid FindingId, Guid OwnerId)> ActionOverdueFindings);
