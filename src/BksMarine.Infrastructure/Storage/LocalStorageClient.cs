using BksMarine.Core.Domain.Ports;

namespace BksMarine.Infrastructure.Storage;

/// <summary>Dev adapter: salva base64 em disco e retorna a URL local. Produção: Supabase Storage (D6).</summary>
public sealed class LocalStorageClient : IStorageClient
{
    private readonly string _directory;

    public LocalStorageClient(string baseDirectory)
    {
        _directory = Path.Combine(baseDirectory, "uploads");
        Directory.CreateDirectory(_directory);
    }

    public Task<string> SaveAsync(string base64Content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(base64Content))
            throw new ArgumentException("Photo content is empty.");

        var bytes = Convert.FromBase64String(base64Content);
        var fileName = $"{Guid.NewGuid():N}.jpg";
        var path = Path.Combine(_directory, fileName);
        File.WriteAllBytes(path, bytes);
        return Task.FromResult($"/uploads/{fileName}");
    }
}
