namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// Kockázati domén/kategória — fix enum v1-ben (ld. docs/design/risk-register-design.md
/// §1.16/1, §2.2). Az érték-lista véglegesítve a 4. lépcsőn (2026-09-06):
/// `RegulatoryExposure` a `Compliance` modullal való névütközés elkerülése miatt
/// átnevezve (eredetileg "megfelelőségi" domain).
/// </summary>
public enum RiskDomain
{
    IT,
    Financial,
    ESG,
    Operational,
    RegulatoryExposure,
    ThirdParty,
    Strategic,
    Reputational
}
