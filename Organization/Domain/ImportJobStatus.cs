namespace Rezilio.Modules.Organization.Domain;

/// <summary>
/// ImportJob státuszgép:
/// Pending → Validating → Valid → Importing → Completed
///                      → Invalid
///                                            → Failed
/// </summary>
public enum ImportJobStatus
{
    Pending,
    Validating,
    Valid,
    Invalid,
    Importing,
    Completed,
    Failed
}
