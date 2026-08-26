using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain;

public sealed class KeyPerson : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Title { get; private set; }
    public string? Department { get; private set; }
    public Guid? OrgUnitId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? BackupPersonName { get; private set; }
    public string? Description { get; private set; }

    private KeyPerson() { }

    public static KeyPerson Create(
        Guid tenantId,
        string name,
        string? title = null,
        string? department = null,
        Guid? orgUnitId = null,
        string? email = null,
        string? phone = null,
        string? backupPersonName = null,
        string? description = null)
    {
        return new KeyPerson
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Title = title,
            Department = department,
            OrgUnitId = orgUnitId,
            Email = email,
            Phone = phone,
            BackupPersonName = backupPersonName,
            Description = description
        };
    }

    public void Update(
        string name,
        string? title = null,
        string? department = null,
        Guid? orgUnitId = null,
        string? email = null,
        string? phone = null,
        string? backupPersonName = null,
        string? description = null)
    {
        Name = name;
        Title = title;
        Department = department;
        OrgUnitId = orgUnitId;
        Email = email;
        Phone = phone;
        BackupPersonName = backupPersonName;
        Description = description;
    }
}
