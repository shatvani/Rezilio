namespace Rezilio.Modules.Organization.Application.Commands.SetOrganizationalUnitAnnualEbitda;

/// <summary>
/// Additív command (ld. docs/design/risk-register-design.md §2.3/§3.3): egy adott szervezeti
/// egység éves EBITDA felülírásának beállítására/törlésére szolgál, külön a meglévő
/// UpdateOrganizationalUnit hívási felülettől.
///
/// <c>Amount=null</c> esetén a felülírás törlődik — az EbitdaBaselineLookupQuery ilyenkor
/// a tenant-szintű alapértékre esik vissza (nem hibaállapot).
/// </summary>
public sealed record SetOrganizationalUnitAnnualEbitdaCommand(Guid OrgUnitId, decimal? Amount, string? Currency);
