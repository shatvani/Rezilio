namespace Rezilio.Modules.Licensing.Application.Commands.CreateTenantLicense;

public sealed record CreateTenantLicenseCommand(
    Guid TenantId,
    SubscriptionPlan Plan,
    DateTimeOffset? ExpiresAt = null);
