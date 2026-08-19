using System.Text.RegularExpressions;

namespace BksMarine.Core.Domain.Locations;

public sealed class PortCode : IEquatable<PortCode>
{
    private static readonly Regex Pattern = new(@"^[A-Z0-9]+$", RegexOptions.Compiled);

    public string Value { get; }

    public PortCode(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (!Pattern.IsMatch(normalized))
            throw new ArgumentException("Invalid port code.", nameof(value));

        Value = normalized;
    }

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value.Trim().ToUpperInvariant());

    public bool Equals(PortCode? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as PortCode);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
