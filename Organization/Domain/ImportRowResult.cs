namespace Rezilio.Modules.Organization.Domain;

/// <summary>
/// Egy Excel sor import eredménye — JSONB-ben tárolva az ImportJob-on.
/// </summary>
public sealed record ImportRowResult(
    int RowNumber,
    bool IsSuccess,
    string? ErrorMessage = null,
    string? ColumnName = null);
