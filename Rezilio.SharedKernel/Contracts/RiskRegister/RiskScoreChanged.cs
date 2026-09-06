namespace Rezilio.SharedKernel.Contracts.RiskRegister;

/// <summary>
/// Modulhatáron átnyúló, "fire-and-forget" domain esemény (Wolverine
/// <c>IMessageBus.PublishAsync</c>-kel kiküldve, nem <c>InvokeAsync</c> — nincs válasz-elvárás)
/// — a RiskRegister modul AssessmentApprovedHandler-je küldi, amikor egy Assessment
/// jóváhagyása megváltoztatja egy Risk reziduális kockázati pontszámát. Ld.
/// docs/design/risk-register-design.md §2.2/§3.3.
///
/// **v1 állapot (2026-09-06):** jelenleg NINCS feliratkozó — a Monitoring/Reporting modul
/// még nem létezik (jövőbeli fázis). A kontraktus és a kiküldés már most megvan, hogy az
/// Assessment-fázis event-dispatch mechanizmusa (ld. §2.5) a végleges, tervezett alakban
/// épüljön fel, ne kelljen később visszamenőleg módosítani az AssessmentApprovedHandler-t.
/// A `ResidualBand` szándékosan string (nem a RiskRegister-belüli RiskScoreBand enum), hogy
/// a SharedKernel kontraktus ne függjön egyetlen konkrét modul domain-típusától sem.
/// </summary>
public sealed record RiskScoreChanged(Guid TenantId, Guid RiskId, int ResidualScore, string ResidualBand);
