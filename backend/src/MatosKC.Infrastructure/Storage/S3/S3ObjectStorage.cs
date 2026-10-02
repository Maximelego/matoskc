namespace MatosKC.Infrastructure.Storage.S3;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using MatosKC.Application.Files.Ports;

public sealed class S3ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 S3Client;
    private readonly S3Options Options;
    private readonly SemaphoreSlim InitializationLock = new(1, 1);
    private bool IsReady;

    public S3ObjectStorage(
        IAmazonS3 s3Client,
        S3Options options
    )
    {
        S3Client = s3Client;
        Options = options;
    }

    public async Task EnsureReadyAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (IsReady)
        {
            return;
        }

        await InitializationLock.WaitAsync(cancellationToken);

        try
        {
            if (IsReady)
            {
                return;
            }

            bool bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(
                S3Client,
                Options.BucketName
            );

            if (!bucketExists)
            {
                await S3Client.PutBucketAsync(
                    new PutBucketRequest
                    {
                        BucketName = Options.BucketName
                    },
                    cancellationToken
                );
            }

            IsReady = true;
        }
        finally
        {
            InitializationLock.Release();
        }
    }

    public async Task StoreAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureReadyAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = Options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,

            // Compatibilité avec Garage et les implémentations S3 locales.
            UseChunkEncoding = false,
        };

        await S3Client.PutObjectAsync(
            request,
            cancellationToken
        );
    }

    public async Task<StoredObject?> RetrieveAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            GetObjectResponse response =
                await S3Client.GetObjectAsync(
                    Options.BucketName,
                    objectKey,
                    cancellationToken
                );

            return new StoredObject(
                response.ResponseStream,
                response.Headers.ContentType
                    ?? "application/octet-stream",
                response.ContentLength
            );
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode ==
                  System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        await S3Client.DeleteObjectAsync(
            Options.BucketName,
            objectKey,
            cancellationToken
        );
    }
}
