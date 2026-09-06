namespace Rezilio.Modules.Organization.Application.Commands.SetTenantDefaultAnnualEbitda;

/// <summary>
/// Additív command (nem az UpdateTenantSettings bővítése — ld. docs/design/risk-register-design.md
/// §2.3/§3.3): a tenant-szintű alapértelmezett éves EBITDA beállítására/törlésére szolgál,
/// külön a meglévő TenantSettings mezőktől, hogy a már meglévő UpdateTenantSettings hívási
/// felület (frontend, tesztek) érintetlen maradjon.
///
/// <c>Amount=null</c> esetén a tenant tudatosan törli/nem adja meg az alapértéket
/// (RiskRegister EbitdaBaselineLookupQuery ilyenkor null-t kap, ha nincs OrgUnit/
/// BusinessProcess-szintű felülírás sem — ez nem hibaállapot).
/// </summary>
public sealed record SetTenantDefaultAnnualEbitdaCommand(decimal? Amount, string? Currency);
