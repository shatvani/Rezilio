using Rezilio.Modules.RiskRegister.Domain.Events;
using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.RiskRegister.Domain;

/// <summary>
/// A kockázat azonosságát és alaptörzsadatát hordozó aggregátum — ld.
/// docs/design/risk-register-design.md §2.2. Tudatosan minimális és domain-agnosztikus:
/// nem tud konkrét eszközökről/beszállítókról/pénzügyi számokról, ezek az Assessment
/// aggregátum (később, §3.3) vagy az Organization modul felelőssége.
///
/// Auditálható (AuditableAggregateRoot), mert az ADR-014 audit-elv a teljes
/// RiskRegister modulra kiterjed.
/// </summary>
public sealed class Risk : AuditableAggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = default!;
    public RiskDomain Domain { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public Guid? OwnerId { get; private set; }
    public RiskStatus Status { get; private set; }

    /// <summary>Kitöltve, ha ez a Risk egy archivált Risk másolásával jött létre (ld. CreateFromArchived).</summary>
    public Guid? CopiedFromRiskId { get; private set; }

    private Risk() { }

    /// <summary>
    /// Új Risk létrehozása Draft állapotban. A Code-ot a hívó (Application-réteg,
    /// CreateRiskHandler) generálja a tenant-szintű sorszámozás alapján — ez nem
    /// aggregátum-invariáns, mert DB-lekérdezést igényel.
    /// </summary>
    public static Risk Create(
        Guid tenantId,
        string code,
        RiskDomain domain,
        string title,
        string description,
        Guid? ownerId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("A Code nem lehet üres.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A Title nem lehet üres.", nameof(title));
        }

        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Domain = domain,
            Title = title,
            Description = description,
            OwnerId = ownerId,
            Status = RiskStatus.Draft
        };

        risk.RaiseDomainEvent(new RiskCreated(risk.Id, tenantId, code, domain));
        return risk;
    }

    /// <summary>
    /// Új Risk létrehozása egy Archived forrás-Risk másolásával (CreateRiskFromArchivedRisk
    /// Command, ld. §2.2 "Archived terminális" pontosítás). Az OwnerId-t tudatosan NEM
    /// veszi át a forrásból. A source-nak Archived állapotúnak kell lennie — ez a
    /// forrás-Risk aggregátum saját invariánsa, ezért itt (a domain rétegben) ellenőrizzük,
    /// nem csak a Command-validátorban.
    /// </summary>
    public static Risk CreateFromArchived(
        Risk source,
        string code,
        RiskDomain? domain,
        string? title,
        string? description)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Status != RiskStatus.Archived)
        {
            throw new InvalidOperationException(
                "CreateRiskFromArchivedRisk csak Archived állapotú Risk-ből hozható létre.");
        }

        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            TenantId = source.TenantId,
            Code = code,
            Domain = domain ?? source.Domain,
            Title = string.IsNullOrWhiteSpace(title) ? source.Title : title,
            Description = string.IsNullOrWhiteSpace(description) ? source.Description : description,
            OwnerId = null,
            Status = RiskStatus.Draft,
            CopiedFromRiskId = source.Id
        };

        risk.RaiseDomainEvent(new RiskCreated(risk.Id, risk.TenantId, code, risk.Domain));
        return risk;
    }

    /// <summary>
    /// Title/Description módosítása. A Domain szándékosan nem módosítható itt (ld. §3.1
    /// UpdateRisk döntés) — téves domain-besorolás javítása archiválás +
    /// CreateRiskFromArchivedRisk útján történik, más doménnel.
    /// </summary>
    public void Update(string title, string description)
    {
        EnsureNotTerminal("A Risk módosítása");

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A Title nem lehet üres.", nameof(title));
        }

        Title = title;
        Description = description;
        RaiseDomainEvent(new RiskUpdated(Id, TenantId));
    }

    /// <summary>
    /// Felelős kijelölése/cseréje. A kapcsolódó KeyPerson létezés/aktív-ellenőrzés
    /// Application-rétegbeli felelősség (ld. §2.5 KeyPersonLookupQuery), nem
    /// aggregátum-invariáns.
    /// </summary>
    public void AssignOwner(Guid ownerId)
    {
        EnsureNotTerminal("Felelős hozzárendelése");

        OwnerId = ownerId;
        RaiseDomainEvent(new RiskOwnerAssigned(Id, TenantId, ownerId));
    }

    /// <summary>Csak Active vagy Treated állapotból hívható (ld. §2.2 állapottáblázat).</summary>
    public void Close()
    {
        if (Status is not (RiskStatus.Active or RiskStatus.Treated))
        {
            throw new InvalidOperationException("A Risk csak Active vagy Treated állapotból zárható le.");
        }

        var oldStatus = Status;
        Status = RiskStatus.Closed;
        RaiseDomainEvent(new RiskStatusChanged(Id, TenantId, oldStatus, Status));
    }

    /// <summary>
    /// Csak Closed állapotból hívható. Az Archived valóban terminális — innen nincs
    /// visszalépés (ld. §2.2 pontosítás, CreateRiskFromArchivedRisk az "újranyitás" módja).
    /// </summary>
    public void Archive()
    {
        if (Status != RiskStatus.Closed)
        {
            throw new InvalidOperationException("A Risk csak Closed állapotból archiválható.");
        }

        Status = RiskStatus.Archived;
        RaiseDomainEvent(new RiskArchived(Id, TenantId));
    }

    private void EnsureNotTerminal(string actionDescription)
    {
        if (Status is RiskStatus.Closed or RiskStatus.Archived)
        {
            throw new InvalidOperationException($"{actionDescription} Closed/Archived állapotú Risk-en nem lehetséges.");
        }
    }
}
