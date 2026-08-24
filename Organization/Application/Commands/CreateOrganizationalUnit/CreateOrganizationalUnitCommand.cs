namespace Rezilio.Modules.Organization.Application.Commands.CreateOrganizationalUnit;

public sealed record CreateOrganizationalUnitCommand(
    Guid TenantId,
    string Name,
    string Code,
    Guid? ParentId = null,
    string? Description = null);
