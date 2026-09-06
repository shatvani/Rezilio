namespace Rezilio.SharedKernel.Contracts.Organization;

/// <summary>
/// Modulközi Wolverine-üzenet (szinkron, in-process request/reply
/// <c>IMessageBus.InvokeAsync&lt;KeyPersonLookupResult&gt;</c>-vel hívva) — más fizikai modulok
/// (pl. RiskRegister) ezzel ellenőrzik, hogy egy KeyPersonId létezik-e és aktív-e, anélkül
/// hogy közvetlenül hozzáférnének az Organization modul DbContext-jéhez (ADR-001).
///
/// A kontraktus a SharedKernelben él, mert mindkét oldalnak (Organization mint válaszadó,
/// a hívó modulok mint kérdező) ismernie kell — ez az első ilyen modulközi szinkron
/// üzenet a kódbázisban (a korábbi <c>Licensing.CheckModuleAccess</c> egy DI-interfészes
/// minta, nem valódi Wolverine-üzenet — a két minta egységesítése dokumentált, nem
/// blokkoló technikai adósság, lásd docs/design/risk-register-design.md §2.5/§2.8).
/// </summary>
public sealed record KeyPersonLookupQuery(Guid TenantId, Guid KeyPersonId);

/// <summary>
/// A <see cref="KeyPersonLookupQuery"/> válasza. <c>Exists=false</c> esetén <c>IsActive</c>
/// jelentés nélküli (mindig <c>false</c>) — a hívónak előbb az <c>Exists</c>-et kell
/// ellenőriznie.
/// </summary>
public sealed record KeyPersonLookupResult(bool Exists, bool IsActive);
