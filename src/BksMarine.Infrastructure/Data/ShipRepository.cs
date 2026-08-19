using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class ShipRepository : IShipRepository
{
    private readonly string _connectionString;

    public ShipRepository(string connectionString) => _connectionString = connectionString;

    public async Task<Ship?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<Ship>(
            "SELECT id, name, loa, dwt, is_active AS IsActive FROM ships WHERE id = @Id", new { Id = id });
    }

    public async Task<Ship?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return await connection.QueryFirstOrDefaultAsync<Ship>(
            "SELECT id, name, loa, dwt, is_active AS IsActive FROM ships WHERE name = @Name", new { Name = name });
    }

    public async Task<List<Ship>> ListAsync(bool activeOnly, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var sql = activeOnly
            ? "SELECT id, name, loa, dwt, is_active AS IsActive FROM ships WHERE is_active = TRUE ORDER BY name"
            : "SELECT id, name, loa, dwt, is_active AS IsActive FROM ships ORDER BY name";
        return (await connection.QueryAsync<Ship>(sql)).ToList();
    }

    public async Task AddAsync(Ship ship, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            "INSERT INTO ships (id, name, loa, dwt, is_active) VALUES (@Id, @Name, @Loa, @Dwt, @IsActive)",
            new { ship.Id, ship.Name, ship.Loa, ship.Dwt, ship.IsActive });
    }

    public async Task UpdateAsync(Ship ship, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            "UPDATE ships SET name = @Name, loa = @Loa, dwt = @Dwt, is_active = @IsActive WHERE id = @Id",
            new { ship.Id, ship.Name, ship.Loa, ship.Dwt, ship.IsActive });
    }
}
