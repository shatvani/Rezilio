namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// Az Assessment jóváhagyási állapotgépe — ld. docs/design/risk-register-design.md §2.2:
/// Draft → Submitted, Submitted → Approved VAGY Submitted → Rejected, Rejected → Draft
/// (átdolgozás — új Draft-tá "reset", nem egy különálló "újranyitott" állapot).
/// Approved és a "lezárt" Rejected→Draft utáni újra-Draft egyaránt a Draft értéket használja,
/// tehát az állapotgép nem különbözteti meg az "első" és "átdolgozott" Draft-ot.
/// </summary>
public enum ApprovalStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}
