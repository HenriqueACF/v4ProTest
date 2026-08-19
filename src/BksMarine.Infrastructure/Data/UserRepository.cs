using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class UserRepository : IUserRepository
{
    private const string AccountSelect = """
        SELECT u.id AS Id,
               u.name AS Name,
               u.job_title AS JobTitle,
               u.email AS Email,
               u.password_hash AS PasswordHash,
               u.profile_id AS ProfileId,
               u.is_active AS IsActive,
               p.name AS ProfileName
        FROM users u
        JOIN profiles p ON p.id = u.profile_id
        """;

    private readonly string _connectionString;

    public UserRepository(string connectionString) => _connectionString = connectionString;

    public async Task<UserAccount?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var row = await connection.QueryFirstOrDefaultAsync<UserRow>(
            AccountSelect + " WHERE u.email = @Email", new { Email = email.Value });
        return row is null ? null : await MapAsync(connection, row);
    }

    public async Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var row = await connection.QueryFirstOrDefaultAsync<UserRow>(
            AccountSelect + " WHERE u.id = @Id", new { Id = id });
        return row is null ? null : await MapAsync(connection, row);
    }

    public async Task<List<UserAccount>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = (activeOnly ? AccountSelect + " WHERE u.is_active = TRUE" : AccountSelect)
            + " ORDER BY u.name LIMIT @Limit OFFSET @Offset";
        var rows = await connection.QueryAsync<UserRow>(sql, new
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        });

        var accounts = new List<UserAccount>();
        foreach (var row in rows)
            accounts.Add(await MapAsync(connection, row));
        return accounts;
    }

    public async Task<int> CountAsync(bool activeOnly, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var sql = activeOnly ? "SELECT COUNT(*) FROM users WHERE is_active = TRUE" : "SELECT COUNT(*) FROM users";
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task UpdatePasswordAsync(Guid userId, PasswordHash hash, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash WHERE id = @Id", new { Id = userId, Hash = hash.Value });
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            INSERT INTO users (id, name, job_title, email, password_hash, profile_id, is_active)
            VALUES (@Id, @Name, @JobTitle, @Email, @PasswordHash, @ProfileId, @IsActive)
            """,
            new
            {
                user.Id,
                user.Name,
                user.JobTitle,
                Email = user.Email.Value,
                PasswordHash = user.PasswordHash.Value,
                user.ProfileId,
                user.IsActive
            });
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            UPDATE users
            SET name = @Name, job_title = @JobTitle, profile_id = @ProfileId, is_active = @IsActive
            WHERE id = @Id
            """,
            new { user.Id, user.Name, user.JobTitle, user.ProfileId, user.IsActive });
    }

    public async Task<Profile?> GetProfileByIdAsync(Guid profileId, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var row = await connection.QueryFirstOrDefaultAsync<ProfileRow>(
            "SELECT id AS Id, name AS Name FROM profiles WHERE id = @Id", new { Id = profileId });
        return row is null ? null : await MapProfileAsync(connection, row);
    }

    public async Task<List<Profile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ProfileRow>("SELECT id AS Id, name AS Name FROM profiles ORDER BY name");

        var profiles = new List<Profile>();
        foreach (var row in rows)
            profiles.Add(await MapProfileAsync(connection, row));
        return profiles;
    }

    private static async Task<UserAccount> MapAsync(NpgsqlConnection connection, UserRow row)
    {
        var modules = (await connection.QueryAsync<string>(
            "SELECT module FROM profile_modules WHERE profile_id = @ProfileId", new { row.ProfileId }))
            .Select(m => Enum.Parse<Module>(m))
            .ToList();

        var profile = new Profile(row.ProfileId, Enum.Parse<ProfileName>(row.ProfileName), modules);
        var user = new User(row.Id, row.Name, row.JobTitle, new Email(row.Email), new PasswordHash(row.PasswordHash), row.ProfileId, row.IsActive);
        return new UserAccount(user, profile);
    }

    private static async Task<Profile> MapProfileAsync(NpgsqlConnection connection, ProfileRow row)
    {
        var modules = (await connection.QueryAsync<string>(
            "SELECT module FROM profile_modules WHERE profile_id = @ProfileId", new { row.Id }))
            .Select(m => Enum.Parse<Module>(m))
            .ToList();
        return new Profile(row.Id, Enum.Parse<ProfileName>(row.Name), modules);
    }

    private sealed record UserRow(
        Guid Id, string Name, string? JobTitle, string Email, string PasswordHash, Guid ProfileId, bool IsActive, string ProfileName);

    private sealed record ProfileRow(Guid Id, string Name);
}
