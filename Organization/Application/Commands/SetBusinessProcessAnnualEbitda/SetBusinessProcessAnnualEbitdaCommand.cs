namespace Rezilio.Modules.Organization.Application.Commands.SetBusinessProcessAnnualEbitda;

/// <summary>
/// Additív command (ld. docs/design/risk-register-design.md §2.3/§3.3): egy adott üzleti
/// folyamat éves EBITDA felülírásának beállítására/törlésére szolgál, külön a meglévő
/// UpdateBusinessProcess hívási felülettől.
///
/// <c>Amount=null</c> esetén a felülírás törlődik — az EbitdaBaselineLookupQuery ilyenkor
/// a tenant-szintű alapértékre esik vissza (nem hibaállapot).
/// </summary>
public sealed record SetBusinessProcessAnnualEbitdaCommand(Guid BusinessProcessId, decimal? Amount, string? Currency);
