namespace Rezilio.Modules.Organization.Application.Commands.AddSupportedLanguage;

public sealed record AddSupportedLanguageCommand(Guid TenantId, string LanguageCode);
