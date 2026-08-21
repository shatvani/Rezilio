namespace Rezilio.Modules.Organization.Application.Commands.UpdateTenantSettings;

public sealed record UpdateTenantSettingsCommand(
    Guid TenantId,
    string DefaultCurrency,
    string DefaultLanguage,
    string Locale,
    string TimeZone);
