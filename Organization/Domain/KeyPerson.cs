using Rezilio.Modules.Organization.Domain.Events;
using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Organization.Domain;

public sealed class KeyPerson : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Title { get; private set; }
    public string? Department { get; private set; }
    public Guid? OrgUnitId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? BackupPersonName { get; private set; }
    public string? Description { get; private set; }

    // Soft-delete: 2026-09-06, a RiskRegister modul KeyPersonLookupQuery-je igényli, hogy
    // egy már hivatkozott KeyPerson deaktiválható legyen anélkül, hogy a más modulokban
    // (RiskRegister: Risk.OwnerId, Control.OwnerId, Action.AssignedTo) élő hivatkozások
    // árvává válnának. A korábbi kemény törlés helyett a DeleteKeyPersonHandler mostantól
    // ezt hívja.
    public bool IsActive { get; private set; } = true;

    // Identitás-kötés: 2026-09-06, a RiskRegister rekord-szintű tulajdonos-ellenőrzéséhez
    // (§3.5) szükség van arra, hogy a bejelentkezett Keycloak-felhasználót (AppClaims.UserId)
    // egy KeyPerson.Id-hoz lehessen kötni. A kötés vagy automatikus (JIT, email-egyezés
    // alapján, ld. ICurrentUserContext.GetKeyPersonIdAsync), vagy Admin által kézzel
    // (LinkKeyPersonToUser Command), ha az email-alapú egyezés nem egyértelmű.
    public string? UserId { get; private set; }

    private KeyPerson() { }

    public static KeyPerson Create(
        Guid tenantId,
        string name,
        string code,
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
            Code = code.Trim().ToUpperInvariant(),
            Title = title,
            Department = department,
            OrgUnitId = orgUnitId,
            Email = email,
            Phone = phone,
            BackupPersonName = backupPersonName,
            Description = description,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string code,
        string? title = null,
        string? department = null,
        Guid? orgUnitId = null,
        string? email = null,
        string? phone = null,
        string? backupPersonName = null,
        string? description = null)
    {
        Name = name;
        Code = code.Trim().ToUpperInvariant();
        Title = title;
        Department = department;
        OrgUnitId = orgUnitId;
        Email = email;
        Phone = phone;
        BackupPersonName = backupPersonName;
        Description = description;
    }

    /// <summary>
    /// Deaktiválja a kulcsszemélyt (soft-delete a korábbi kemény törlés helyett).
    /// A meglévő, más modulokból (pl. RiskRegister) mutató hivatkozások érvényben
    /// maradnak, de a KeyPersonLookupQuery ettől kezdve IsActive=false-t ad vissza rá —
    /// új hozzárendelés (pl. AssignRiskOwner) nem fogadja el.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        RaiseDomainEvent(new KeyPersonDeactivated(Id, TenantId));
    }

    /// <summary>
    /// Összeköti ezt a KeyPerson-t egy Keycloak-felhasználóval (identitás-kötés).
    /// Idempotens: ha már ugyanaz a UserId van rajta, nem csinál semmit és nem
    /// dob újabb eseményt.
    /// </summary>
    public void LinkToUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("A UserId nem lehet üres.", nameof(userId));
        }

        if (UserId == userId)
        {
            return;
        }

        UserId = userId;
        RaiseDomainEvent(new KeyPersonLinkedToUser(Id, TenantId, userId));
    }
}
