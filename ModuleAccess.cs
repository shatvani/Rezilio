namespace Rezilio.Modules.Licensing.Domain;

public sealed record ModuleAccess(ModuleType Module, bool IsActive, DateTimeOffset? TrialEndsAt)
{
    public bool IsAccessible => IsActive && (TrialEndsAt is null || TrialEndsAt > DateTimeOffset.UtcNow);
}
