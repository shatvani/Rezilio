using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain;

public sealed class Customer : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Industry { get; private set; }
    public string? Country { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Description { get; private set; }

    private Customer() { }

    public static Customer Create(
        Guid tenantId,
        string name,
        string code,
        string? industry = null,
        string? country = null,
        string? contactEmail = null,
        string? contactPhone = null,
        string? description = null)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code.Trim().ToUpperInvariant(),
            Industry = industry,
            Country = country,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            Description = description
        };
    }

    public void Update(
        string name,
        string code,
        string? industry = null,
        string? country = null,
        string? contactEmail = null,
        string? contactPhone = null,
        string? description = null)
    {
        Name = name;
        Code = code.Trim().ToUpperInvariant();
        Industry = industry;
        Country = country;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Description = description;
    }
}
