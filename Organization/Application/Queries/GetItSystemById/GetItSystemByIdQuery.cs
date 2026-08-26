using Organization.Domain;

namespace Organization.Application.Queries.GetItSystemById;

public sealed record GetItSystemByIdQuery(Guid Id, Guid TenantId);

public sealed record ItSystemDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    ItSystemType Type,
    HostingType HostingType,
    CriticalityLevel CriticalityLevel,
    string? Vendor,
    string? Version,
    Guid? OwnerId,
    List<Guid> SupportedOrgUnitIds);
