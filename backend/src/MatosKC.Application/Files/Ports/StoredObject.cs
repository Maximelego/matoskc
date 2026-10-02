namespace MatosKC.Application.Files.Ports;

public sealed record StoredObject(
    Stream Content,
    string ContentType,
    long ContentLength
) : IAsyncDisposable
{
    public ValueTask DisposeAsync()
    {
        return Content.DisposeAsync();
    }
}
