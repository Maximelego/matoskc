namespace MatosKC.Application.Tests.Fakes;

using MatosKC.Application.Files.Ports;

internal sealed class FakeImageContentValidator : IImageContentValidator
{
    public bool Reject { get; set; }
    public int Calls { get; private set; }

    public void Validate(byte[] content, string contentType)
    {
        Calls++;

        if (Reject)
        {
            throw new ArgumentException("Invalid test image.");
        }
    }
}
