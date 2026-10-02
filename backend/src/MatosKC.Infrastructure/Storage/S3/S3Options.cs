namespace MatosKC.Infrastructure.Storage.S3;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public required string ServiceUrl { get; init; }
    public required string Region { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string BucketName { get; init; }

    public bool ForcePathStyle { get; init; } = true;
}
