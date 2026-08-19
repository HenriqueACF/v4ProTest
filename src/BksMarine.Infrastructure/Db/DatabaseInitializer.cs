using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Db;

public sealed class SeedAdminOptions
{
    public string Email { get; init; } = "admin@bksmarine.com";
    public string Password { get; init; } = "Admin@123";
}

public sealed class SeedDemoOptions
{
    public bool Enabled { get; init; }
}

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;
    private readonly IPasswordHasher _hasher;
    private readonly SeedAdminOptions _seed;
    private readonly bool _seedDemoEnabled;

    public DatabaseInitializer(
        string connectionString,
        IPasswordHasher hasher,
        SeedAdminOptions seed,
        bool seedDemoEnabled)
    {
        _connectionString = connectionString;
        _hasher = hasher;
        _seed = seed;
        _seedDemoEnabled = seedDemoEnabled;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            "CREATE TABLE IF NOT EXISTS schema_migrations (version TEXT PRIMARY KEY, applied_at TIMESTAMPTZ NOT NULL DEFAULT now())");

        var applied = (await connection.QueryAsync<string>("SELECT version FROM schema_migrations")).ToHashSet();
        foreach (var migration in Migrations.All.Where(m => !applied.Contains(m.Version)))
        {
            await using var tx = await connection.BeginTransactionAsync(ct);
            await connection.ExecuteAsync(migration.Sql);
            await connection.ExecuteAsync("INSERT INTO schema_migrations (version) VALUES (@Version)", new { migration.Version });
            await tx.CommitAsync(ct);
        }

        await SeedAdminAsync(connection, ct);

        if (_seedDemoEnabled)
            await SeedDemoAsync(connection, ct);
    }

    private async Task SeedAdminAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
        if (count > 0)
            return;

        var hash = _hasher.Hash(_seed.Password).Value;
        await connection.ExecuteAsync(
            """
            INSERT INTO users (id, name, email, password_hash, profile_id, is_active)
            VALUES (@Id, 'Administrador', @Email, @Hash, (SELECT id FROM profiles WHERE name = 'Full'), TRUE)
            """,
            new { Id = Guid.NewGuid(), Email = _seed.Email, Hash = hash });
    }

    private async Task SeedDemoAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var portCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ports");
        if (portCount > 0)
            return;

        var santos = Guid.NewGuid();
        var rio = Guid.NewGuid();

        await connection.ExecuteAsync(
            "INSERT INTO ports (id, name, code, address, is_active) VALUES (@Id, @Name, @Code, @Address, TRUE)",
            new[]
            {
                new { Id = santos, Name = "Santos", Code = "SAN", Address = "Av. Portuária, 100" },
                new { Id = rio, Name = "Rio de Janeiro", Code = "RIO", Address = "Av. Rodrigues Alves, 200" }
            });

        await connection.ExecuteAsync(
            """
            INSERT INTO berths (id, name, port_id, max_loa, max_dwt, type, is_active) VALUES
                (@Id1, 'Berth 01', @Port, 300, 80000, 'Cargo', TRUE),
                (@Id2, 'Berth 02', @Port, 250, 50000, 'Mixed', TRUE)
            """,
            new[]
            {
                new { Id1 = Guid.NewGuid(), Id2 = Guid.NewGuid(), Port = santos },
                new { Id1 = Guid.NewGuid(), Id2 = Guid.NewGuid(), Port = rio }
            });

        await connection.ExecuteAsync(
            """
            INSERT INTO ships (id, name, loa, dwt, is_active) VALUES
                (@Id1, 'Pomone', 128, 5000, TRUE),
                (@Id2, 'Grande Rio', 240, 45000, TRUE)
            """,
            new { Id1 = Guid.NewGuid(), Id2 = Guid.NewGuid() });
    }
}
