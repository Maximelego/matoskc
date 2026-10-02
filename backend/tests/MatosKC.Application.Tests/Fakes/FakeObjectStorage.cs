namespace MatosKC.Application.Tests.Fakes;

using MatosKC.Application.Files.Ports;

internal sealed class FakeObjectStorage : IObjectStorage
{
    private readonly Dictionary<string, (byte[] Content, string ContentType)> Objects = [];

    public IReadOnlyCollection<string> ObjectKeys => Objects.Keys;

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

        StoredObject result = new(
            new MemoryStream(stored.Content, writable: false),
            stored.ContentType,
            stored.Content.LongLength
        );
        return Task.FromResult<StoredObject?>(result);
    }

    public Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        Objects.Remove(objectKey);
        return Task.CompletedTask;
    }
}
