using BksMarine.Core.Domain.Ports;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class LoginAttemptRepository : ILoginAttemptRepository
{
    private readonly string _connectionString;

    public LoginAttemptRepository(string connectionString) => _connectionString = connectionString;

    public async Task RegisterAsync(string email, bool success, DateTime attemptedAt, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            "INSERT INTO login_attempts (email, attempted_at, success) VALUES (@Email, @AttemptedAt, @Success)",
            new { Email = email, AttemptedAt = attemptedAt, Success = success });
    }

    public async Task<int> CountRecentFailuresAsync(string email, DateTime since, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM login_attempts WHERE email = @Email AND success = FALSE AND attempted_at >= @Since",
            new { Email = email, Since = since });
    }

    public async Task ClearAsync(string email, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync("DELETE FROM login_attempts WHERE email = @Email", new { Email = email });
    }
}
