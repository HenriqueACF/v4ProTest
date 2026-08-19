using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Db;

public sealed class SeedAdminOptions
{
    public string Email { get; init; } = "admin@bksmarine.com";
    public string Password { get; init; } = "Admin@123";
}

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly IPasswordHasher _hasher;
    private readonly SeedAdminOptions _seed;

    public DatabaseInitializer(string connectionString, IPasswordHasher hasher, SeedAdminOptions seed)
    {
        _connectionString = connectionString;
        _hasher = hasher;
        _seed = seed;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(Schema.Sql);

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
        if (count > 0)
            return;

        var hash = _hasher.Hash(_seed.Password).Value;
        await connection.ExecuteAsync(
            """
            INSERT INTO users (id, email, password_hash, profile_id, is_active)
            VALUES (@Id, @Email, @Hash, (SELECT id FROM profiles WHERE name = 'Full'), TRUE)
            """,
            new { Id = Guid.NewGuid(), Email = _seed.Email, Hash = hash });
    }
}
