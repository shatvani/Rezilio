namespace Rezilio.Modules.Organization.Application.Services;

/// <summary>
/// Egy Excel oszlop leírása — template generáláshoz és validációhoz egyaránt.
/// </summary>
public sealed record ImportColumnDefinition(
    string Name,
    bool IsRequired,
    string ExampleValue,
    string? Description = null);
