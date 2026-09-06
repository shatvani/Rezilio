namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>A megállapítás eredete — ld. docs/design/risk-register-design.md §2.2.</summary>
public enum FindingSource
{
    Audit,
    SelfAssessment,
    Incident,
    External
}
