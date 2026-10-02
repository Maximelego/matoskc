namespace MatosKC.Infrastructure;

using Amazon.Runtime;
using Amazon.S3;
using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using MatosKC.Infrastructure.Persistence.Seeding;
using MatosKC.Infrastructure.Storage.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString
        );

        services.AddDbContext<MatosKCDbContext>(
            options => options.UseNpgsql(connectionString)
        );

        services.AddScoped<
            IEquipmentCategoryRepository,
            EquipmentCategoryRepository
        >();

        services.AddScoped<
            IEquipmentRepository,
            EquipmentRepository
        >();

        services.AddScoped<
            IEquipmentPhotoRepository,
            EquipmentPhotoRepository
        >();

        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }

    public static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options = new S3Options
        {
            ServiceUrl =
                configuration["S3:ServiceUrl"]
                ?? throw new InvalidOperationException(
                    "S3:ServiceUrl is missing."
                ),

            Region =
                configuration["S3:Region"]
                ?? throw new InvalidOperationException(
                    "S3:Region is missing."
                ),

            AccessKey =
                configuration["S3:AccessKey"]
                ?? throw new InvalidOperationException(
                    "S3:AccessKey is missing."
                ),

            SecretKey =
                configuration["S3:SecretKey"]
                ?? throw new InvalidOperationException(
                    "S3:SecretKey is missing."
                ),

            BucketName =
                configuration["S3:BucketName"]
                ?? throw new InvalidOperationException(
                    "S3:BucketName is missing."
                ),

            ForcePathStyle =
                !bool.TryParse(
                    configuration["S3:ForcePathStyle"],
                    out bool forcePathStyle
                ) || forcePathStyle
        };

        services.AddSingleton(options);

        services.AddSingleton<IAmazonS3>(
            _ =>
            {
                var credentials = new BasicAWSCredentials(
                    options.AccessKey,
                    options.SecretKey
                );

                var s3Configuration = new AmazonS3Config
                {
                    ServiceURL = options.ServiceUrl,
                    AuthenticationRegion = options.Region,
                    ForcePathStyle = options.ForcePathStyle
                };

                return new AmazonS3Client(
                    credentials,
                    s3Configuration
                );
            }
        );

        services.AddScoped<IObjectStorage, S3ObjectStorage>();

        return services;
    }
}
