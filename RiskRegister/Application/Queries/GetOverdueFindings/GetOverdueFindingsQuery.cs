namespace Rezilio.Modules.RiskRegister.Application.Queries.GetOverdueFindings;

/// <summary>
/// Belső lekérdezés (ld. §3.2): melyik Finding-ek léptek túl a monitoring-küszöbön —
/// 3 munkanap Open-ben triázs nélkül (FindingTriageOverdue), vagy 7 naptári nap
/// Triaged-ben Risk/Action nélkül (FindingActionOverdue). Ezt egy időzített
/// háttérfolyamat hívná periodikusan és a visszaadott Id-kra hívná meg a Finding
/// MarkTriageOverdue()/MarkActionOverdue() metódusait — a háttérfolyamat maga
/// (Wolverine recurring job vagy hasonló) egyelőre nincs bekötve, ld. a Finding-fázis
/// lezáró review-ját a docs/design/risk-register-design.md-ben.
/// </summary>
public static class GetOverdueFindingsQuery
{
    private const int TriageOverdueBusinessDays = 3;
    private const int ActionOverdueCalendarDays = 7;

    public static async Task<OverdueFindingsResult> ExecuteAsync(RiskRegisterDbContext db, Guid tenantId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var triageThreshold = SubtractBusinessDays(now, TriageOverdueBusinessDays);
        var actionThreshold = now.AddDays(-ActionOverdueCalendarDays);

        var triageOverdueIds = await db.Findings
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId && f.Status == FindingStatus.Open && f.CreatedAt <= triageThreshold)
            .Select(f => f.Id)
            .ToListAsync(ct);

        var actionOverdueCandidates = await db.Findings
            .AsNoTracking()
            .Where(f => f.TenantId == tenantId
                     && f.Status == FindingStatus.Triaged
                     && f.OwnerId != null
                     && f.LastModified != null
                     && f.LastModified <= actionThreshold)
            .Select(f => new { f.Id, OwnerId = f.OwnerId!.Value })
            .ToListAsync(ct);

        return new OverdueFindingsResult(
            triageOverdueIds,
            actionOverdueCandidates.Select(x => (x.Id, x.OwnerId)).ToList());
    }

    /// <summary>Egyszerű munkanap-számítás (hétvégét kihagyja); ünnepnapokat nem vesz figyelembe v1-ben.</summary>
    private static DateTimeOffset SubtractBusinessDays(DateTimeOffset from, int businessDays)
    {
        var date = from;
        var remaining = businessDays;

        while (remaining > 0)
        {
            date = date.AddDays(-1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                remaining--;
            }
        }

        return date;
    }
}
