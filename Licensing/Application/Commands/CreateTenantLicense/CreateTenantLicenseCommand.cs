using Rezilio.Modules.Licensing.Domain;

namespace Licensing.Application.Commands.CreateTenantLicense;

public sealed record CreateTenantLicenseCommand(
    Guid TenantId,
    SubscriptionPlan Plan,
    DateTimeOffset? ExpiresAt = null);
