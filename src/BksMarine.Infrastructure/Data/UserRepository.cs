using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class UserRepository : IUserRepository
{
    private const string Sql = """
        SELECT u.id AS Id,
               u.email AS Email,
               u.password_hash AS PasswordHash,
               u.profile_id AS ProfileId,
               u.is_active AS IsActive,
               p.name AS ProfileName
        FROM users u
        JOIN profiles p ON p.id = u.profile_id
        WHERE u.email = @Email
        """;

    private readonly string _connectionString;

    public UserRepository(string connectionString) => _connectionString = connectionString;

    public async Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var row = await connection.QueryFirstOrDefaultAsync<UserRow>(Sql, new { Email = email.Value });
        if (row is null)
            return null;

        var modules = (await connection.QueryAsync<string>(
            "SELECT module FROM profile_modules WHERE profile_id = @ProfileId",
            new { row.ProfileId }))
            .Select(m => Enum.Parse<Module>(m))
            .ToList();

        var profile = new Profile(row.ProfileId, Enum.Parse<ProfileName>(row.ProfileName), modules);
        var user = new User(row.Id, new Email(row.Email), new PasswordHash(row.PasswordHash), row.ProfileId, row.IsActive);
        return new UserAccount(user, profile);
    }

    private sealed record UserRow(
        Guid Id,
        string Email,
        string PasswordHash,
        Guid ProfileId,
        bool IsActive,
        string ProfileName);
}
