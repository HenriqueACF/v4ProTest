namespace BksMarine.Core.Domain.Users;

public sealed class PasswordHash : IEquatable<PasswordHash>
{
    public string Value { get; }

    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Password hash cannot be empty.", nameof(value));

        Value = value;
    }

    public bool Equals(PasswordHash? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as PasswordHash);

    public override int GetHashCode() => Value.GetHashCode();
}
