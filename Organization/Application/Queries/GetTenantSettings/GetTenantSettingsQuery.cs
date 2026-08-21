namespace Rezilio.Modules.Organization.Application.Queries.GetTenantSettings;

public sealed record GetTenantSettingsQuery(Guid TenantId);

public sealed record TenantSettingsResult(
    Guid TenantId,
    string DefaultCurrency,
    string DefaultLanguage,
    string Locale,
    string TimeZone,
    IReadOnlyList<string> SupportedLanguages);
