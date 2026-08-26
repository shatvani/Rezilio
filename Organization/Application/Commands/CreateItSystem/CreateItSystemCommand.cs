using Organization.Domain;

namespace Organization.Application.Commands.CreateItSystem;

public sealed record CreateItSystemCommand(
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
