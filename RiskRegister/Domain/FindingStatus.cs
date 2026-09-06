namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// ld. §2.2 állapotgép: Open → Triaged, majd Triaged → Linked VAGY Triaged →
/// ActionCreated (kölcsönösen kizárják egymást), végül → Closed. Open-ből közvetlenül
/// Closed-ba nem lehet lépni. Closed terminális.
/// </summary>
public enum FindingStatus
{
    Open,
    Triaged,
    Linked,
    ActionCreated,
    Closed
}
