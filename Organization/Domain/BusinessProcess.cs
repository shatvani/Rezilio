using Rezilio.Modules.Organization.Domain.Events;
using Rezilio.SharedKernel.DDD;
using Rezilio.SharedKernel.DDD.VOs;
using Rezilio.SharedKernel.Results;

namespace Rezilio.Modules.Organization.Domain;

public sealed class BusinessProcess : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public CriticalityLevel CriticalityLevel { get; private set; }
    public Guid? OwnerId { get; private set; }
    public Guid? OrgUnitId { get; private set; }
    public int? MaxTolerableDowntimeMinutes { get; private set; }
    public int? RecoveryTimeObjectiveMinutes { get; private set; }
    public List<Guid> DependsOnSystemIds { get; private set; } = [];

    /// <summary>
    /// Üzleti folyamat szintű éves EBITDA felülírás (ld. docs/design/risk-register-design.md
    /// §2.3) — az OrganizationalUnit-szintű és tenant-szintű alapértékek elé sorolva az
    /// EbitdaBaselineLookupQuery prioritás-logikájában. Null-lal is hívható (nincs felülírás megadva).
    /// </summary>
    public Money? AnnualEbitda { get; private set; }

    private BusinessProcess() { }

    public static Result<BusinessProcess> Create(
        Guid tenantId,
        string code,
        string name,
        string category,
        CriticalityLevel criticalityLevel,
        Guid? ownerId = null,
        Guid? orgUnitId = null,
        int? maxTolerableDowntimeMinutes = null,
        int? recoveryTimeObjectiveMinutes = null,
        List<Guid>? dependsOnSystemIds = null)
    {
        if (recoveryTimeObjectiveMinutes.HasValue && maxTolerableDowntimeMinutes.HasValue
            && recoveryTimeObjectiveMinutes.Value > maxTolerableDowntimeMinutes.Value)
        {
            return Result.Failure<BusinessProcess>(
                "Recovery Time Objective cannot be greater than Max Tolerable Downtime.");
        }

        var bp = new BusinessProcess
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name,
            Category = category,
            CriticalityLevel = criticalityLevel,
            OwnerId = ownerId,
            OrgUnitId = orgUnitId,
            MaxTolerableDowntimeMinutes = maxTolerableDowntimeMinutes,
            RecoveryTimeObjectiveMinutes = recoveryTimeObjectiveMinutes,
            DependsOnSystemIds = dependsOnSystemIds ?? []
        };
        bp.RaiseDomainEvent(new BusinessProcessCreated(bp.Id, tenantId));
        return Result.Success(bp);
    }

    public Result Update(
        string code,
        string name,
        string category,
        CriticalityLevel criticalityLevel,
        Guid? ownerId = null,
        Guid? orgUnitId = null,
        int? maxTolerableDowntimeMinutes = null,
        int? recoveryTimeObjectiveMinutes = null,
        List<Guid>? dependsOnSystemIds = null)
    {
        if (recoveryTimeObjectiveMinutes.HasValue && maxTolerableDowntimeMinutes.HasValue
            && recoveryTimeObjectiveMinutes.Value > maxTolerableDowntimeMinutes.Value)
        {
            return Result.Failure(
                "Recovery Time Objective cannot be greater than Max Tolerable Downtime.");
        }

        Code = code.Trim().ToUpperInvariant();
        Name = name;
        Category = category;
        CriticalityLevel = criticalityLevel;
        OwnerId = ownerId;
        OrgUnitId = orgUnitId;
        MaxTolerableDowntimeMinutes = maxTolerableDowntimeMinutes;
        RecoveryTimeObjectiveMinutes = recoveryTimeObjectiveMinutes;
        DependsOnSystemIds = dependsOnSystemIds ?? [];
        return Result.Success();
    }

    /// <summary>Null-lal is hívható — ez azt jelenti, hogy ezen az üzleti folyamaton nincs EBITDA-felülírás (ld. §2.3).</summary>
    public void SetAnnualEbitda(Money? annualEbitda)
    {
        AnnualEbitda = annualEbitda;
    }
}
