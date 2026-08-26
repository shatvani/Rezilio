using Organization.Domain;

namespace Organization.Application.Commands.UpdateItSystem;

public sealed record UpdateItSystemCommand(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    ItSystemType Type,
    HostingType HostingType,
    CriticalityLevel CriticalityLevel,
    string? Vendor = null,
    string? Version = null,
    Guid? OwnerId = null,
    List<Guid>? SupportedOrgUnitIds = null);
