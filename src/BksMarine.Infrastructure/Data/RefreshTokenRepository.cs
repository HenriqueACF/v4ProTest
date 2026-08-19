using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Users;
using Dapper;
using Npgsql;

namespace BksMarine.Infrastructure.Data;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly string _connectionString;

    public RefreshTokenRepository(string connectionString) => _connectionString = connectionString;

    public async Task SaveAsync(RefreshToken token, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            """
            INSERT INTO refresh_tokens (id, user_id, token_hash, expires_at, revoked_at, created_at)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @RevokedAt, now())
            """,
            new { token.Id, token.UserId, token.TokenHash, token.ExpiresAt, token.RevokedAt });
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        var row = await connection.QueryFirstOrDefaultAsync<RefreshTokenRow>(
            "SELECT id, user_id AS UserId, token_hash AS TokenHash, expires_at AS ExpiresAt, revoked_at AS RevokedAt FROM refresh_tokens WHERE token_hash = @TokenHash",
            new { TokenHash = tokenHash });
        return row is null ? null : new RefreshToken(row.Id, row.UserId, row.TokenHash, row.ExpiresAt, row.RevokedAt);
    }

    public async Task RevokeAsync(Guid id, DateTime revokedAt, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(
            "UPDATE refresh_tokens SET revoked_at = @RevokedAt WHERE id = @Id", new { Id = id, RevokedAt = revokedAt });
    }

    private sealed record RefreshTokenRow(Guid Id, Guid UserId, string TokenHash, DateTime ExpiresAt, DateTime? RevokedAt);
}
