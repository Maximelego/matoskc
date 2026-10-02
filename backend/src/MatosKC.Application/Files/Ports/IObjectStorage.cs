namespace MatosKC.Application.Files.Ports;

public interface IObjectStorage
{
    public Task StoreAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    public Task<StoredObject?> RetrieveAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    );

    public Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    );
}
