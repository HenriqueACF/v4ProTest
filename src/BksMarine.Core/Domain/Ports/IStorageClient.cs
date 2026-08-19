namespace BksMarine.Core.Domain.Ports;

public interface IStorageClient
{
    Task<string> SaveAsync(string base64Content, CancellationToken ct = default);
}
