namespace Rezilio.Modules.RiskRegister.Application.Services;

/// <summary>
/// Risk.Code generálása: "RISK-{év}-{tenant-szintű, éven belüli sorszám}" (ld. §3.1).
/// A pontos, verseny-helyzet-mentes mechanizmus (pl. DB-szintű szekvencia) implementációs
/// finomítás — v1-ben egy egyszerű COUNT-alapú számláló + a hívó oldali (unique index +
/// retry) védelem elegendő, mivel a Risk létrehozása nem egy nagy-forgalmú, verseny-
/// érzékeny művelet.
/// </summary>
public static class RiskCodeGenerator
{
    public static async Task<string> GenerateNextCodeAsync(RiskRegisterDbContext db, Guid tenantId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RISK-{year}-";

        var countThisYear = await db.Risks
            .CountAsync(r => r.TenantId == tenantId && r.Code.StartsWith(prefix), ct);

        return $"{prefix}{(countThisYear + 1):D5}";
    }
}
