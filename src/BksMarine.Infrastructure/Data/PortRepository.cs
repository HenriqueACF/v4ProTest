using BksMarine.Core.Domain.Locations;
using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class PortRepository : IPortRepository
{
    private readonly string _connectionString;

    public PortRepository(string connectionString) => _connectionString = connectionString;

    public async Task<Port?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var row = await connection.QueryFirstOrDefaultAsync<PortRow>(
            "SELECT * FROM ports WHERE id = @Id", new { Id = id });
        return row is null ? null : Map(row);
    }

    public async Task<Port?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var row = await connection.QueryFirstOrDefaultAsync<PortRow>(
            "SELECT * FROM ports WHERE code = @Code", new { Code = code.ToUpperInvariant() });
        return row is null ? null : Map(row);
    }

    public async Task<List<Port>> ListAsync(bool activeOnly, int page, int pageSize, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = activeOnly
            ? "SELECT * FROM ports WHERE is_active = TRUE ORDER BY name LIMIT @Limit OFFSET @Offset"
            : "SELECT * FROM ports ORDER BY name LIMIT @Limit OFFSET @Offset";

        var rows = await connection.QueryAsync<PortRow>(sql, new
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        });
        return rows.Select(Map).ToList();
    }

    public async Task<int> CountAsync(bool activeOnly, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var sql = activeOnly ? "SELECT COUNT(*) FROM ports WHERE is_active = TRUE" : "SELECT COUNT(*) FROM ports";
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task AddAsync(Port port, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            INSERT INTO ports (id, name, code, address, contact, notes, is_active)
            VALUES (@Id, @Name, @Code, @Address, @Contact, @Notes, @IsActive)
            """,
            new { port.Id, port.Name, Code = port.Code.Value, port.Address, port.Contact, port.Notes, port.IsActive });
    }

    public async Task UpdateAsync(Port port, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await connection.ExecuteAsync(
            """
            UPDATE ports
            SET name = @Name, code = @Code, address = @Address, contact = @Contact, notes = @Notes, is_active = @IsActive
            WHERE id = @Id
            """,
            new { port.Id, port.Name, Code = port.Code.Value, port.Address, port.Contact, port.Notes, port.IsActive });
    }

    private static Port Map(PortRow row) =>
        new(row.Id, row.Name, new PortCode(row.Code), row.Address, row.Contact, row.Notes, row.IsActive);

    private sealed record PortRow(
        Guid Id, string Name, string Code, string? Address, string? Contact, string? Notes, bool IsActive);
}
