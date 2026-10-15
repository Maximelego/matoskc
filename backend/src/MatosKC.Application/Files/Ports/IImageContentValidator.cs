namespace MatosKC.Application.Files.Ports;

public interface IImageContentValidator
{
    void Validate(byte[] content, string contentType);
}
