using Rezilio.Modules.Licensing.Domain.Events;
using Rezilio.SharedKernel.DDD;

namespace Rezilio.Modules.Licensing.Domain;

public sealed class TenantLicense : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public SubscriptionPlan Plan { get; private set; }
    public DateTimeOffset? PlanExpiresAt { get; private set; }

    private readonly List<ModuleAccess> _moduleAccesses = [];
    public IReadOnlyList<ModuleAccess> ModuleAccesses => _moduleAccesses.AsReadOnly();

    // EF Core proxy ctor
    private TenantLicense() { }

    public static TenantLicense Create(Guid tenantId, SubscriptionPlan plan, DateTimeOffset? expiresAt = null)
    {
        var license = new TenantLicense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Plan = plan,
            PlanExpiresAt = expiresAt
        };

        foreach (ModuleType module in GetDefaultModules(plan))
        {
            license._moduleAccesses.Add(new ModuleAccess(module, IsActive: true, TrialEndsAt: null));
        }

        return license;
    }

    public void ActivateModule(ModuleType module)
    {
        Replace(module, isActive: true, trialEndsAt: null);
        RaiseDomainEvent(new ModuleActivated(TenantId, module));
    }

    public void DeactivateModule(ModuleType module)
    {
        Replace(module, isActive: false, trialEndsAt: null);
    }

    public void StartTrial(ModuleType module)
    {
        DateTimeOffset trialEnd = DateTimeOffset.UtcNow.AddDays(14);
        Replace(module, isActive: true, trialEndsAt: trialEnd);
        RaiseDomainEvent(new ModuleActivated(TenantId, module));
    }

    public bool IsModuleActive(ModuleType module)
    {
        ModuleAccess? access = _moduleAccesses.FirstOrDefault(m => m.Module == module);
        return access?.IsAccessible ?? false;
    }

    private void Replace(ModuleType module, bool isActive, DateTimeOffset? trialEndsAt)
    {
        ModuleAccess? existing = _moduleAccesses.FirstOrDefault(m => m.Module == module);
        if (existing is not null)
        {
            _moduleAccesses.Remove(existing);
        }
        _moduleAccesses.Add(new ModuleAccess(module, isActive, trialEndsAt));
    }

    private static IEnumerable<ModuleType> GetDefaultModules(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => [ModuleType.RiskRegister, ModuleType.Assessment, ModuleType.Treatment],
        SubscriptionPlan.Professional => [
            ModuleType.RiskRegister, ModuleType.Assessment, ModuleType.Treatment,
            ModuleType.Monitoring, ModuleType.Incidents],
        SubscriptionPlan.Enterprise => Enum.GetValues<ModuleType>(),
        _ => []
    };
}
