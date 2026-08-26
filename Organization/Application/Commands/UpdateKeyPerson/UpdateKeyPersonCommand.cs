namespace Rezilio.Modules.Organization.Application.Commands.UpdateKeyPerson;

public sealed record UpdateKeyPersonCommand(
    Guid Id,
    string Name,
    string? Title,
    string? Department,
    Guid? OrgUnitId,
    string? Email,
    string? Phone,
    string? BackupPersonName,
    string? Description);
