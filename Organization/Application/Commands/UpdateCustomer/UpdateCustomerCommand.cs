namespace Rezilio.Modules.Organization.Application.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Code,
    string? Industry,
    string? Country,
    string? ContactEmail,
    string? ContactPhone,
    string? Description);
