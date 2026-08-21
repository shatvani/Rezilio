using System.Text.RegularExpressions;

namespace Rezilio.SharedKernel.DDD.VOs;

public sealed record LanguageCode
{
    // BCP 47: pl. "en", "hu", "en-US", "hu-HU"
    private static readonly Regex _bcp47Pattern =
        new(@"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})*$", RegexOptions.Compiled);

    public string Value { get; }

    public LanguageCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A nyelvkód nem lehet üres.");
        }

        if (!_bcp47Pattern.IsMatch(value))
        {
            throw new ArgumentException($"Érvénytelen BCP 47 nyelvkód: '{value}'.");
        }

        Value = value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(LanguageCode code) => code.Value;

    public static readonly LanguageCode Hungarian = new("hu");
    public static readonly LanguageCode English = new("en");
}
