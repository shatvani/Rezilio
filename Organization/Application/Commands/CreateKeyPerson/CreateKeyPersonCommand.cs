namespace Rezilio.Modules.Organization.Application.Commands.CreateKeyPerson;

public sealed record CreateKeyPersonCommand(
    Guid TenantId,
    string Name,
    string? Title,
    string? Department,
    Guid? OrgUnitId,
    string? Email,
    string? Phone,
    string? BackupPersonName,
    string? Description);
