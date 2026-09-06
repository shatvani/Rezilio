namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// A Risk állapotgépe — ld. docs/design/risk-register-design.md §2.2 állapottáblázat.
/// Az Archived valóban terminális (nincs Archived → Closed visszalépés); egy archivált
/// Risk "újranyitása" a CreateRiskFromArchivedRisk Command-dal egy ÚJ Risk-et hoz létre
/// (CopiedFromRiskId hivatkozással), nem élesztjük fel a régit.
/// </summary>
public enum RiskStatus
{
    Draft,
    UnderReview,
    Active,
    Treated,
    Closed,
    Archived
}
