using Rezilio.Modules.RiskRegister.Domain.Events;
using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// Egy nyers megállapítás/hiányosság — triázs-pont a folyamat elején, mielőtt eldőlne,
/// hogy önálló Risk-et érdemel-e, egy meglévő Risk tünete-e, vagy elég hozzá egy egyszerű
/// javítás (Action). Ld. docs/design/risk-register-design.md §2.2.
/// </summary>
public sealed class Finding : AuditableAggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public FindingSource Source { get; private set; }
    public string Description { get; private set; } = default!;
    public FindingSeverity Severity { get; private set; }
    public FindingStatus Status { get; private set; }

    /// <summary>Kitöltve, ha CreateRiskFromFinding vagy LinkFindingToExistingRisk megtörtént. Egyszer beállítva nem módosítható.</summary>
    public Guid? LinkedRiskId { get; private set; }

    /// <summary>Nullable Open-ben; a TriageFinding kötelezően kitölti (ld. §2.2 OwnerId nullability-szabály).</summary>
    public Guid? OwnerId { get; private set; }

    private Finding() { }

    public static Finding Create(Guid tenantId, FindingSource source, string description, FindingSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A Description nem lehet üres.", nameof(description));
        }

        var finding = new Finding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Source = source,
            Description = description,
            Severity = severity,
            Status = FindingStatus.Open
        };

        finding.RaiseDomainEvent(new FindingCreated(finding.Id, tenantId, source, severity));
        return finding;
    }

    /// <summary>
    /// Open → Triaged. Az OwnerId kötelező (ld. §2.2 — a triázs egy atomi
    /// döntés+felelős-kijelölés lépés). A KeyPerson létezés/aktív-ellenőrzés
    /// Application-rétegbeli felelősség (KeyPersonLookupQuery).
    /// </summary>
    public void Triage(Guid ownerId)
    {
        if (Status != FindingStatus.Open)
        {
            throw new InvalidOperationException("Csak Open állapotú Finding triázsolható.");
        }

        OwnerId = ownerId;
        Status = FindingStatus.Triaged;
        RaiseDomainEvent(new FindingTriaged(Id, TenantId, ownerId));
    }

    /// <summary>
    /// Triaged → Linked. A LinkedRiskId egyszer beállítva nem módosítható (ld. §2.2
    /// invariáns) — mivel a Status ezután már nem Triaged, egy második hívás úgyis
    /// elutasításra kerül, ez a metódus explicit védelemként is ellenőrzi.
    /// </summary>
    public void LinkToRisk(Guid riskId)
    {
        if (Status != FindingStatus.Triaged)
        {
            throw new InvalidOperationException("Csak Triaged állapotú Finding köthető Risk-hez.");
        }

        if (LinkedRiskId is not null)
        {
            throw new InvalidOperationException("A LinkedRiskId egyszer beállítva nem módosítható.");
        }

        LinkedRiskId = riskId;
        Status = FindingStatus.Linked;
        RaiseDomainEvent(new FindingLinkedToRisk(Id, TenantId, riskId));
    }

    /// <summary>
    /// Triaged → ActionCreated. Az Action aggregátum a TreatmentPlan+Control+Action
    /// fázisban készül el — ezt a metódust a CreateActionFromFinding Command fogja
    /// hívni, ami tudatosan egy későbbi fázisra van halasztva (ld. §3.2). A metódus
    /// már most definiálva van a domain-teljesség kedvéért, jelenleg nincs hívója.
    /// </summary>
    public void MarkActionCreated(Guid actionId)
    {
        if (Status != FindingStatus.Triaged)
        {
            throw new InvalidOperationException("Csak Triaged állapotú Finding-hoz hozható létre Action.");
        }

        Status = FindingStatus.ActionCreated;
        RaiseDomainEvent(new FindingActionCreated(Id, TenantId, actionId));
    }

    /// <summary>Csak Linked vagy ActionCreated állapotból hívható (ld. §2.2 — Open-ből közvetlenül nem).</summary>
    public void Close()
    {
        if (Status is not (FindingStatus.Linked or FindingStatus.ActionCreated))
        {
            throw new InvalidOperationException("A Finding csak Linked vagy ActionCreated állapotból zárható le.");
        }

        Status = FindingStatus.Closed;
        RaiseDomainEvent(new FindingClosed(Id, TenantId));
    }

    /// <summary>A monitoring-háttérfolyamat hívja (mechanizmus még nincs bekötve, ld. §3.2).</summary>
    public void MarkTriageOverdue()
    {
        if (Status != FindingStatus.Open)
        {
            throw new InvalidOperationException("Csak Open állapotú Finding jelezhető triázs-túllépettként.");
        }

        RaiseDomainEvent(new FindingTriageOverdue(Id, TenantId));
    }

    /// <summary>A monitoring-háttérfolyamat hívja (mechanizmus még nincs bekötve, ld. §3.2).</summary>
    public void MarkActionOverdue()
    {
        if (Status != FindingStatus.Triaged || OwnerId is null)
        {
            throw new InvalidOperationException("Csak Triaged állapotú, felelőssel rendelkező Finding jelezhető action-túllépettként.");
        }

        RaiseDomainEvent(new FindingActionOverdue(Id, TenantId, OwnerId.Value));
    }
}
