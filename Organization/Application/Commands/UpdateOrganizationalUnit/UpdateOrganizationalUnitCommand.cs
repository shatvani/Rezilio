namespace Rezilio.Modules.Organization.Application.Commands.UpdateOrganizationalUnit;

public sealed record UpdateOrganizationalUnitCommand(
    Guid Id,
    string Name,
    string Code,
    Guid? ParentId = null,
    string? Description = null);
