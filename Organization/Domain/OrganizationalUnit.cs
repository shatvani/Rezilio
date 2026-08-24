using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain;

public sealed class OrganizationalUnit : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public string? Description { get; private set; }

    // EF Core proxy ctor
    private OrganizationalUnit() { }

    public static OrganizationalUnit Create(
        Guid tenantId,
        string name,
        string code,
        Guid? parentId = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return new OrganizationalUnit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            ParentId = parentId,
            Description = description?.Trim()
        };
    }

    public void Update(
        string name,
        string code,
        Guid? parentId = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        ParentId = parentId;
        Description = description?.Trim();
    }
}
