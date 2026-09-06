namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// Négyfokozatú súlyossági skála (ld. §2.2 — végleges lista, 2026-09-06). A Severity
/// szerinti differenciált monitoring-küszöb (pl. Critical-nál rövidebb határidő) v1-ben
/// nincs bevezetve — a FindingTriageOverdue egységesen 3 munkanap után vált ki,
/// Severity-től függetlenül (ld. §3.2).
/// </summary>
public enum FindingSeverity
{
    Low,
    Medium,
    High,
    Critical
}
