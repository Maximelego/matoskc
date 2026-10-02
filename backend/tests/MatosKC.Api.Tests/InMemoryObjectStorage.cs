namespace MatosKC.Api.Tests;

using System.Collections.Concurrent;
using MatosKC.Application.Files.Ports;

internal sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> Objects = new();

    public Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task StoreAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        Objects[objectKey] = (buffer.ToArray(), contentType);
    }

    public Task<StoredObject?> RetrieveAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        if (!Objects.TryGetValue(objectKey, out var stored))
        {
            return Task.FromResult<StoredObject?>(null);
        }

        return Task.FromResult<StoredObject?>(
            new StoredObject(
                new MemoryStream(stored.Content, writable: false),
                stored.ContentType,
                stored.Content.LongLength
            )
        );
    }

    public Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        Objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }
}
