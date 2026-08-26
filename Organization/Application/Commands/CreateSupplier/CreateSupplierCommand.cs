namespace Rezilio.Modules.Organization.Application.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(
    Guid TenantId,
    string Name,
    string Code,
    string? Industry,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string? Description);
