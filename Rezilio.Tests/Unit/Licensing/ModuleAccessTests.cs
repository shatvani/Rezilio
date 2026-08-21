using Rezilio.Modules.Licensing.Domain;
using Xunit;

namespace Rezilio.Tests.Unit.Licensing;

public class ModuleAccessTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void IsAccessible_WhenActiveAndNoTrialEnd_ReturnsTrue()
    {
        ModuleAccess access = new(ModuleType.RiskRegister, IsActive: true, TrialEndsAt: null);
        Assert.True(access.IsAccessible);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsAccessible_WhenNotActive_ReturnsFalse()
    {
        ModuleAccess access = new(ModuleType.RiskRegister, IsActive: false, TrialEndsAt: null);
        Assert.False(access.IsAccessible);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsAccessible_WhenTrialExpired_ReturnsFalse()
    {
        ModuleAccess access = new(
            ModuleType.RiskRegister,
            IsActive: true,
            TrialEndsAt: DateTimeOffset.UtcNow.AddDays(-1));
        Assert.False(access.IsAccessible);
    }
}
