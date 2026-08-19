using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class BerthRepository : IBerthRepository
{
    private readonly string _connectionString;

    public BerthRepository(string connectionString) => _connectionString = connectionString;

    public async Task<Berth?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var row = await connection.QueryFirstOrDefaultAsync<BerthRow>(
            "SELECT * FROM berths WHERE id = @Id", new { Id = id });
        return row is null ? null : Map(row);
    }

    public async Task<Berth?> GetByNameInPortAsync(string name, Guid portId, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var row = await connection.QueryFirstOrDefaultAsync<BerthRow>(
            "SELECT * FROM berths WHERE name = @Name AND port_id = @PortId",
            new { Name = name, PortId = portId });
        return row is null ? null : Map(row);
    }

    public async Task<List<Berth>> ListByPortAsync(Guid portId, bool activeOnly, int page, int pageSize, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = activeOnly
            ? "SELECT * FROM berths WHERE port_id = @PortId AND is_active = TRUE ORDER BY name LIMIT @Limit OFFSET @Offset"
            : "SELECT * FROM berths WHERE port_id = @PortId ORDER BY name LIMIT @Limit OFFSET @Offset";

        var rows = await connection.QueryAsync<BerthRow>(sql, new
        {
            PortId = portId,
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        });
        return rows.Select(Map).ToList();
    }

    public async Task<int> CountByPortAsync(Guid portId, bool activeOnly, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var sql = activeOnly
            ? "SELECT COUNT(*) FROM berths WHERE port_id = @PortId AND is_active = TRUE"
            : "SELECT COUNT(*) FROM berths WHERE port_id = @PortId";
        return await connection.ExecuteScalarAsync<int>(sql, new { PortId = portId });
    }

    public async Task AddAsync(Berth berth, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            INSERT INTO berths (id, name, port_id, max_loa, max_dwt, type, notes, is_active)
            VALUES (@Id, @Name, @PortId, @MaxLoa, @MaxDwt, @Type, @Notes, @IsActive)
            """,
            new
            {
                berth.Id,
                berth.Name,
                berth.PortId,
                berth.MaxLoa,
                berth.MaxDwt,
                Type = berth.Type.ToString(),
                berth.Notes,
                berth.IsActive
            });
    }

    public async Task UpdateAsync(Berth berth, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            UPDATE berths
            SET name = @Name, max_loa = @MaxLoa, max_dwt = @MaxDwt, type = @Type, notes = @Notes, is_active = @IsActive
            WHERE id = @Id
            """,
            new
            {
                berth.Id,
                berth.Name,
                berth.MaxLoa,
                berth.MaxDwt,
                Type = berth.Type.ToString(),
                berth.Notes,
                berth.IsActive
            });
    }

    private static Berth Map(BerthRow row) =>
        new(row.Id, row.Name, row.PortId, row.MaxLoa, row.MaxDwt, Enum.Parse<BerthType>(row.Type), row.Notes, row.IsActive);

    private sealed record BerthRow(
        Guid Id, string Name, Guid PortId, decimal? MaxLoa, decimal? MaxDwt, string Type, string? Notes, bool IsActive);
}
