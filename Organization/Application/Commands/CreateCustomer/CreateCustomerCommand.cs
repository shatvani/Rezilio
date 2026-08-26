namespace Rezilio.Modules.Organization.Application.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    Guid TenantId,
    string Name,
    string Code,
    string? Industry,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string? Description);
