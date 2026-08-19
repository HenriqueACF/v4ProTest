namespace BksMarine.Core.Domain.Ports;

public interface ILoginAttemptRepository
{
    Task RegisterAsync(string email, bool success, DateTime attemptedAt, CancellationToken ct = default);
    Task<int> CountRecentFailuresAsync(string email, DateTime since, CancellationToken ct = default);
    Task ClearAsync(string email, CancellationToken ct = default);
}
