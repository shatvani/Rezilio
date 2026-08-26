namespace Rezilio.Modules.Organization.Application.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string Code,
    string? Industry,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string? Description);
