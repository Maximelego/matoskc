namespace MatosKC.Application.Administration;

public sealed class AdministrationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
