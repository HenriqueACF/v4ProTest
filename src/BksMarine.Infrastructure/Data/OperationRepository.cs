using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class OperationRepository : IOperationRepository
{
    private const string Select = """
        SELECT id, type, ship_id AS ShipId, port_id AS PortId, berth_id AS BerthId,
               agency_name AS AgencyName, pilot_name AS PilotName,
               pilot_boarding_time AS PilotBoardingTime, tug_bow_name AS TugBowName,
               tug_bow_time AS TugBowTime, tug_stern_name AS TugSternName,
               tug_stern_time AS TugSternTime, first_line_time AS FirstLineTime,
               last_line_time AS LastLineTime, draft_bow AS DraftBow,
               draft_midship AS DraftMidship, draft_stern AS DraftStern,
               side, notes, occurred_at AS OccurredAt, undocking_time AS UndockingTime,
               photos, transmission_status AS TransmissionStatus, created_at AS CreatedAt
        FROM operations
        """;

    private readonly string _connectionString;

    public OperationRepository(string connectionString) => _connectionString = connectionString;

    public async Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var row = await connection.QueryFirstOrDefaultAsync<OperationRow>(
            Select + " WHERE id = @Id", new { Id = id });
        return row is null ? null : Map(row);
    }

    public async Task<List<Operation>> ListAsync(OperationType? type, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = Select;
        var where = new List<string>();
        if (type is not null) where.Add("type = @Type");
        if (from is not null) where.Add("occurred_at >= @From");
        if (to is not null) where.Add("occurred_at <= @To");
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY occurred_at DESC";

        var rows = await connection.QueryAsync<OperationRow>(sql, new { Type = type?.ToString(), From = from, To = to });
        return rows.Select(Map).ToList();
    }

    public async Task<List<OperationReportRow>> ListReportAsync(
        OperationType? type, DateTime? from, DateTime? to, Guid? portId, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        var sql = """
            SELECT o.id AS OperationId, o.occurred_at AS OccurredAt, o.type AS Type,
                   s.name AS ShipName, p.name AS PortName, b.name AS BerthName,
                   o.transmission_status AS TransmissionStatus, o.photos AS Photos
            FROM operations o
            JOIN ships s ON s.id = o.ship_id
            JOIN ports p ON p.id = o.port_id
            JOIN berths b ON b.id = o.berth_id
            """;
        var where = new List<string>();
        if (type is not null) where.Add("o.type = @Type");
        if (from is not null) where.Add("o.occurred_at >= @From");
        if (to is not null) where.Add("o.occurred_at <= @To");
        if (portId is not null) where.Add("o.port_id = @PortId");
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += " ORDER BY o.occurred_at DESC";

        var rows = await connection.QueryAsync<ReportRow>(sql, new
        {
            Type = type?.ToString(),
            From = from,
            To = to,
            PortId = portId
        });

        return rows.Select(r => new OperationReportRow(
            r.OperationId, r.OccurredAt, Enum.Parse<OperationType>(r.Type),
            r.ShipName, r.PortName, r.BerthName,
            Enum.Parse<TransmissionStatus>(r.TransmissionStatus),
            r.Photos ?? Array.Empty<string>())).ToList();
    }

    public async Task AddAsync(Operation operation, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            """
            INSERT INTO operations (
                id, type, ship_id, port_id, berth_id, agency_name, pilot_name,
                pilot_boarding_time, tug_bow_name, tug_bow_time, tug_stern_name,
                tug_stern_time, first_line_time, last_line_time, draft_bow,
                draft_midship, draft_stern, side, notes, occurred_at, undocking_time,
                photos, transmission_status, created_at)
            VALUES (
                @Id, @Type, @ShipId, @PortId, @BerthId, @AgencyName, @PilotName,
                @PilotBoardingTime, @TugBowName, @TugBowTime, @TugSternName,
                @TugSternTime, @FirstLineTime, @LastLineTime, @DraftBow,
                @DraftMidship, @DraftStern, @Side, @Notes, @OccurredAt, @UndockingTime,
                @Photos, @TransmissionStatus, @CreatedAt)
            """,
            new
            {
                operation.Id,
                Type = operation.Type.ToString(),
                operation.ShipId,
                operation.PortId,
                operation.BerthId,
                operation.AgencyName,
                operation.PilotName,
                operation.PilotBoardingTime,
                operation.TugBowName,
                operation.TugBowTime,
                operation.TugSternName,
                operation.TugSternTime,
                operation.FirstLineTime,
                operation.LastLineTime,
                operation.DraftBow,
                operation.DraftMidship,
                operation.DraftStern,
                Side = operation.Side?.ToString(),
                operation.Notes,
                operation.OccurredAt,
                operation.UndockingTime,
                Photos = operation.Photos.ToArray(),
                TransmissionStatus = operation.TransmissionStatus.ToString(),
                operation.CreatedAt
            });
    }

    public async Task UpdateAsync(Operation operation, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            "UPDATE operations SET transmission_status = @TransmissionStatus WHERE id = @Id",
            new { Id = operation.Id, TransmissionStatus = operation.TransmissionStatus.ToString() });
    }

    private static Operation Map(OperationRow r) =>
        new(
            r.Id,
            Enum.Parse<OperationType>(r.Type),
            r.ShipId, r.PortId, r.BerthId,
            r.AgencyName, r.PilotName, r.PilotBoardingTime,
            r.TugBowName, r.TugBowTime, r.TugSternName, r.TugSternTime,
            r.FirstLineTime, r.LastLineTime,
            r.DraftBow, r.DraftMidship, r.DraftStern,
            r.Side is null ? null : Enum.Parse<Side>(r.Side),
            r.Notes, r.OccurredAt, r.UndockingTime,
            r.Photos ?? Array.Empty<string>(),
            Enum.Parse<TransmissionStatus>(r.TransmissionStatus),
            r.CreatedAt);

    private sealed record OperationRow(
        Guid Id, string Type, Guid ShipId, Guid PortId, Guid BerthId,
        string? AgencyName, string? PilotName, DateTime? PilotBoardingTime,
        string? TugBowName, DateTime? TugBowTime, string? TugSternName, DateTime? TugSternTime,
        DateTime? FirstLineTime, DateTime? LastLineTime,
        decimal? DraftBow, decimal? DraftMidship, decimal? DraftStern,
        string? Side, string? Notes, DateTime OccurredAt, DateTime? UndockingTime,
        string[]? Photos, string TransmissionStatus, DateTime CreatedAt);

    private sealed record ReportRow(
        Guid OperationId, DateTime OccurredAt, string Type, string ShipName,
        string PortName, string BerthName, string TransmissionStatus, string[]? Photos);
}
