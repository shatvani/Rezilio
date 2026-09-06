using Rezilio.SharedKernel.DDD;
using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Domain;

public sealed class OrganizationalUnit : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public string? Description { get; private set; }

    /// <summary>
    /// Szervezeti egység szintű éves EBITDA felülírás (ld. docs/design/risk-register-design.md
    /// §2.3) — ha be van állítva, ez élvez elsőbbséget a tenant-szintű
    /// TenantSettings.DefaultAnnualEbitda felett az EbitdaBaselineLookupQuery
    /// prioritás-logikájában. Null-lal is hívható (nincs felülírás megadva).
    /// </summary>
    public Money? AnnualEbitda { get; private set; }

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

    /// <summary>Null-lal is hívható — ez azt jelenti, hogy ezen a szervezeti egységen nincs EBITDA-felülírás (ld. §2.3).</summary>
    public void SetAnnualEbitda(Money? annualEbitda)
    {
        AnnualEbitda = annualEbitda;
    }
}
