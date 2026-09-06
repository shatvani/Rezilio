using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.SharedKernel.Contracts.Organization;

/// <summary>
/// Modulközi Wolverine-üzenet (szinkron, in-process request/reply
/// <c>IMessageBus.InvokeAsync&lt;EbitdaBaselineLookupResult&gt;</c>-vel hívva) — a RiskRegister
/// modul (Assessment aggregát, EBITDA-hatás számítás) ezzel kéri le a ténylegesen alkalmazandó
/// éves EBITDA-alapértéket egy adott kontextusra (OrgUnit vagy BusinessProcess szintű Assessment
/// impact-context esetén). A teljes prioritás-logika (OrgUnit/BusinessProcess-szintű felülírás
/// > tenant-szintű TenantSettings.DefaultAnnualEbitda > null) itt, az Organization modulban fut
/// le egyetlen konszolidált válaszban — a RiskRegister modul nem kombinál két külön lekérdezést
/// (ld. docs/design/risk-register-design.md §2.3/§3.3).
///
/// <c>OrgUnitId</c> és <c>BusinessProcessId</c> kölcsönösen kizáróak (ugyanaz az invariáns,
/// mint az Assessment.ImpactContextOrgUnitId/ImpactContextBusinessProcessId esetén) — mindkettő
/// null esetén egyenesen a tenant-szintű alapértékre esik vissza a válasz.
/// </summary>
public sealed record EbitdaBaselineLookupQuery(Guid TenantId, Guid? OrgUnitId, Guid? BusinessProcessId);

/// <summary>
/// Az <see cref="EbitdaBaselineLookupQuery"/> válasza. <c>AnnualEbitda</c> lehet <c>null</c> —
/// ez nem hibaállapot, csupán azt jelenti, hogy sem a kontextus-szintű, sem a tenant-szintű
/// alapérték nincs beállítva. A hívó (RiskRegister EbitdaImpactCalculator) ez esetben
/// <c>null</c> EbitdaImpactPercentage-t számol, nem kivételt dob.
/// </summary>
public sealed record EbitdaBaselineLookupResult(Money? AnnualEbitda);
