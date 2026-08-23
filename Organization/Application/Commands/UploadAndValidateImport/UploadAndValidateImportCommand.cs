using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Application.Commands.UploadAndValidateImport;

public sealed record UploadAndValidateImportCommand(
    Guid TenantId,
    EntityType EntityType,
    byte[] FileContent);
