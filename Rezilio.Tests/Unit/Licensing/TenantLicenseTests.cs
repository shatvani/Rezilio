using Rezilio.Modules.Licensing.Domain;
using Rezilio.Modules.Licensing.Domain.Events;
using Xunit;

namespace Rezilio.Tests.Unit.Licensing;

public class TenantLicenseTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithBasicPlan_ActivatesOnlyBasicModules()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Basic);

        Assert.Equal(_tenantId, license.TenantId);
        Assert.Equal(SubscriptionPlan.Basic, license.Plan);
        Assert.Equal(3, license.ModuleAccesses.Count);
        Assert.True(license.IsModuleActive(ModuleType.RiskRegister));
        Assert.True(license.IsModuleActive(ModuleType.Assessment));
        Assert.True(license.IsModuleActive(ModuleType.Treatment));
        Assert.False(license.IsModuleActive(ModuleType.Monitoring));
        Assert.False(license.IsModuleActive(ModuleType.Compliance));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithProfessionalPlan_ActivatesFiveModules()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Professional);

        Assert.Equal(5, license.ModuleAccesses.Count);
        Assert.True(license.IsModuleActive(ModuleType.Monitoring));
        Assert.True(license.IsModuleActive(ModuleType.Incidents));
        Assert.False(license.IsModuleActive(ModuleType.Compliance));
        Assert.False(license.IsModuleActive(ModuleType.AIInsights));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithEnterprisePlan_ActivatesAllModules()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Enterprise);

        Assert.Equal(Enum.GetValues<ModuleType>().Length, license.ModuleAccesses.Count);
        foreach (ModuleType module in Enum.GetValues<ModuleType>())
        {
            Assert.True(license.IsModuleActive(module));
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ActivateModule_OnInactiveModule_MakesItAccessible_AndRaisesModuleActivated()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Basic);

        license.ActivateModule(ModuleType.Compliance);

        Assert.True(license.IsModuleActive(ModuleType.Compliance));
        ModuleActivated raised = Assert.IsType<ModuleActivated>(Assert.Single(license.DomainEvents));
        Assert.Equal(_tenantId, raised.TenantId);
        Assert.Equal(ModuleType.Compliance, raised.Module);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeactivateModule_OnActiveModule_MakesItInaccessible_AndRaisesNoEvent()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Basic);

        license.DeactivateModule(ModuleType.RiskRegister);

        Assert.False(license.IsModuleActive(ModuleType.RiskRegister));
        Assert.Empty(license.DomainEvents);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void StartTrial_SetsTrialEndTo14DaysFromNow_AndRaisesModuleActivated()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Basic);
        DateTimeOffset before = DateTimeOffset.UtcNow;

        license.StartTrial(ModuleType.AIInsights);

        Assert.True(license.IsModuleActive(ModuleType.AIInsights));

        ModuleAccess access = license.ModuleAccesses.Single(m => m.Module == ModuleType.AIInsights);
        Assert.NotNull(access.TrialEndsAt);
        Assert.InRange(
            access.TrialEndsAt!.Value,
            before.AddDays(14).AddSeconds(-5),
            before.AddDays(14).AddSeconds(5));

        ModuleActivated raised = Assert.IsType<ModuleActivated>(Assert.Single(license.DomainEvents));
        Assert.Equal(ModuleType.AIInsights, raised.Module);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsModuleActive_ForModuleNeverAdded_ReturnsFalse()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Basic);

        Assert.False(license.IsModuleActive(ModuleType.AIInsights));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ActivateModule_Twice_DoesNotDuplicateModuleAccessEntry()
    {
        TenantLicense license = TenantLicense.Create(_tenantId, SubscriptionPlan.Basic);

        license.ActivateModule(ModuleType.Compliance);
        license.ActivateModule(ModuleType.Compliance);

        Assert.Single(license.ModuleAccesses, m => m.Module == ModuleType.Compliance);
    }
}
