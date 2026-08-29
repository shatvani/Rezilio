using Rezilio.Modules.Organization.Domain.Events;
using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain;

public sealed class ItSystem : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public ItSystemType Type { get; private set; }
    public string? Vendor { get; private set; }
    public string? Version { get; private set; }
    public HostingType HostingType { get; private set; }
    public Guid? OwnerId { get; private set; }
    public List<Guid> SupportedOrgUnitIds { get; private set; } = [];
    public CriticalityLevel CriticalityLevel { get; private set; }

    private ItSystem() { }

    public static ItSystem Create(
        Guid tenantId,
        string code,
        string name,
        ItSystemType type,
        HostingType hostingType,
        CriticalityLevel criticalityLevel,
        string? vendor = null,
        string? version = null,
        Guid? ownerId = null,
        List<Guid>? supportedOrgUnitIds = null)
    {
        var system = new ItSystem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name,
            Type = type,
            HostingType = hostingType,
            CriticalityLevel = criticalityLevel,
            Vendor = vendor,
            Version = version,
            OwnerId = ownerId,
            SupportedOrgUnitIds = supportedOrgUnitIds ?? []
        };
        system.RaiseDomainEvent(new ItSystemCreated(system.Id, tenantId));
        return system;
    }

    public void Update(
        string code,
        string name,
        ItSystemType type,
        HostingType hostingType,
        CriticalityLevel criticalityLevel,
        string? vendor = null,
        string? version = null,
        Guid? ownerId = null,
        List<Guid>? supportedOrgUnitIds = null)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name;
        Type = type;
        HostingType = hostingType;
        CriticalityLevel = criticalityLevel;
        Vendor = vendor;
        Version = version;
        OwnerId = ownerId;
        SupportedOrgUnitIds = supportedOrgUnitIds ?? [];
    }
}
