using Rezilio.SharedKernel.DDD;
using Rezilio.SharedKernel.DDD.VOs;

namespace Rezilio.Modules.Organization.Domain;

public sealed class TenantSettings : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public CurrencyCode DefaultCurrency { get; private set; } = default!;
    public LanguageCode DefaultLanguage { get; private set; } = default!;
    public string Locale { get; private set; } = default!;
    public string TimeZone { get; private set; } = default!;

    private readonly List<LanguageCode> _supportedLanguages = [];
    public IReadOnlyList<LanguageCode> SupportedLanguages => _supportedLanguages.AsReadOnly();

    /// <summary>
    /// Tenant-szintű alapértelmezett éves EBITDA (ld. docs/design/risk-register-design.md
    /// §2.3) — manuálisan rögzítve az adminisztrátor által, nem számított. A RiskRegister
    /// modul az EbitdaBaselineLookupQuery-n keresztül olvassa, ha nincs OrgUnit/
    /// BusinessProcess-szintű felülírás.
    /// </summary>
    public Money? DefaultAnnualEbitda { get; private set; }

    // EF Core proxy ctor
    private TenantSettings() { }

    public static TenantSettings Create(
        Guid tenantId,
        CurrencyCode defaultCurrency,
        LanguageCode defaultLanguage,
        string locale,
        string timeZone)
    {
        var settings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DefaultCurrency = defaultCurrency,
            DefaultLanguage = defaultLanguage,
            Locale = locale,
            TimeZone = timeZone
        };

        settings._supportedLanguages.Add(defaultLanguage);
        return settings;
    }

    public void Update(
        CurrencyCode defaultCurrency,
        LanguageCode defaultLanguage,
        string locale,
        string timeZone)
    {
        DefaultCurrency = defaultCurrency;
        DefaultLanguage = defaultLanguage;
        Locale = locale;
        TimeZone = timeZone;

        if (!_supportedLanguages.Contains(defaultLanguage))
        {
            _supportedLanguages.Add(defaultLanguage);
        }
    }

    public void AddSupportedLanguage(LanguageCode language)
    {
        if (!_supportedLanguages.Contains(language))
        {
            _supportedLanguages.Add(language);
        }
    }

    /// <summary>Null-lal is hívható — ez azt jelenti, hogy a tenant tudatosan nem ad meg EBITDA-alapértéket (ld. §2.3).</summary>
    public void SetDefaultAnnualEbitda(Money? annualEbitda)
    {
        DefaultAnnualEbitda = annualEbitda;
    }
}
