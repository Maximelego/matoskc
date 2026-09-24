namespace MatosKC.Application;

using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Get;
using MatosKC.Application.Equipments.List;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services
    )
    {
        services.AddScoped<CreateEquipmentCategoryUseCase>();
        services.AddScoped<GetEquipmentCategoryUseCase>();

        services.AddScoped<CreateEquipmentUseCase>();
        services.AddScoped<GetEquipmentUseCase>();
        services.AddScoped<ListEquipmentUseCase>();

        return services;
    }
}
