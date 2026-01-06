using System.Text.RegularExpressions;

namespace Auth.Domain.ValueObjects;

public sealed record Domain
{
    private static readonly Regex DomainRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    public const int MaxLength = 30;

    public string Value { get; }

    private Domain(string value)
    {
        Value = value;
    }

    public static Domain Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Domain cannot be empty", nameof(value));

        var normalizedValue = value.ToLowerInvariant().Trim();

        if (normalizedValue.Length > MaxLength)
            throw new ArgumentException($"Domain cannot exceed {MaxLength} characters", nameof(value));

        if (!DomainRegex.IsMatch(normalizedValue))
            throw new ArgumentException(
                "Domain must contain only lowercase letters, numbers, and hyphens. " +
                "It cannot start or end with hyphen, and cannot have consecutive hyphens.",
                nameof(value));

        return new Domain(normalizedValue);
    }

    public static implicit operator string(Domain domain) => domain.Value;

    public override string ToString() => Value;

    public static bool TryCreate(string value, out Domain domain)
    {
        domain = null!;

        try
        {
            domain = Create(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = normalized.Replace(' ', '-');
        normalized = Regex.Replace(normalized, @"-+", "-");
        normalized = normalized.Trim('-');

        if (normalized.Length > MaxLength)
            normalized = normalized.Substring(0, MaxLength).TrimEnd('-');

        return normalized;
    }
}