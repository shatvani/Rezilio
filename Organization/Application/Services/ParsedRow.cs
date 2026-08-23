namespace Rezilio.Modules.Organization.Application.Services;

/// <summary>
/// Parser által visszaadott nyers sor — még domain objektummá nem konvertálva.
/// </summary>
public sealed record ParsedRow(
    int RowNumber,
    bool IsValid,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyList<string> Errors);
