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

    /// <summary>
    /// Draft/Active/Treated → UnderReview (ld. §2.2 állapottáblázat, 2026-09-06-i
    /// kiegészítés). A SubmitAssessment (jelen fázis) és a jövőbeli SubmitTreatmentPlan
    /// (TreatmentPlan-fázis) egyaránt ezt hívja modulon belüli event-handleren keresztül,
    /// SOHA nem közvetlenül a Command handlerből (ld. §2.5 esemény-dispatch minta).
    /// </summary>
    public void BeginReview()
    {
        if (Status is not (RiskStatus.Draft or RiskStatus.Active or RiskStatus.Treated))
        {
            throw new InvalidOperationException(
                "A Risk csak Draft, Active vagy Treated állapotból léphet UnderReview-ba.");
        }

        var oldStatus = Status;
        Status = RiskStatus.UnderReview;
        RaiseDomainEvent(new RiskStatusChanged(Id, TenantId, oldStatus, Status));
    }

    /// <summary>
    /// UnderReview → Active. Az AssessmentApprovedHandler hívja — az ApproveAssessment
    /// MINDIG Active-ba viszi a Risk-et, függetlenül a kockázat-étvágy kimenetelétől
    /// (ld. §3.3 üzleti szabály #1; az Active a nyugalmi állapot, függetlenül attól, hogy
    /// van-e éppen aktív TreatmentPlan — a Treated csak a TreatmentPlan-fázisban, egy
    /// külön ApproveTreatmentPlan hívás nyomán következik be).
    /// </summary>
    public void ActivateAfterApproval()
    {
        if (Status != RiskStatus.UnderReview)
        {
            throw new InvalidOperationException("Csak UnderReview állapotú Risk aktiválható jóváhagyás után.");
        }

        var oldStatus = Status;
        Status = RiskStatus.Active;
        RaiseDomainEvent(new RiskStatusChanged(Id, TenantId, oldStatus, Status));
    }

    /// <summary>
    /// UnderReview → Draft VAGY UnderReview → Active, az Assessment elutasítása után
    /// (ld. §2.2 állapottáblázat "UnderReview" sor). Az AssessmentRejectedHandler dönti el
    /// és adja át a <paramref name="hasPriorApprovedAssessment"/> értéket: ha ez volt az
    /// adott Risk ELSŐ Assessment-je (még sosem volt jóváhagyott), akkor Draft-ba esik
    /// vissza (rework az elejéről); ha volt már korábbi jóváhagyott Assessment, akkor a
    /// korábbi értékelés marad érvényben, és a Risk Active-ba esik vissza.
    /// </summary>
    public void RevertAfterRejection(bool hasPriorApprovedAssessment)
    {
        if (Status != RiskStatus.UnderReview)
        {
            throw new InvalidOperationException("Csak UnderReview állapotú Risk eshet vissza elutasítás után.");
        }

        var oldStatus = Status;
        Status = hasPriorApprovedAssessment ? RiskStatus.Active : RiskStatus.Draft;
        RaiseDomainEvent(new RiskStatusChanged(Id, TenantId, oldStatus, Status));
    }

    private void EnsureNotTerminal(string actionDescription)
    {
        if (Status is RiskStatus.Closed or RiskStatus.Archived)
        {
            throw new InvalidOperationException($"{actionDescription} Closed/Archived állapotú Risk-en nem lehetséges.");
        }
    }
}
