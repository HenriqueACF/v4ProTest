using System.Text.RegularExpressions;

namespace BksMarine.Core.Domain.Users;

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex Pattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public string Value { get; }

    public Email(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Invalid email address.", nameof(value));

        Value = value.Trim().ToLowerInvariant();
    }

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value);

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
