namespace MatosKC.Infrastructure;

using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
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

        return services;
    }
}
