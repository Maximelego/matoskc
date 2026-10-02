namespace MatosKC.Application;

using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Application.EquipmentCategories.List;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Get;
using MatosKC.Application.Equipments.List;
using MatosKC.Application.EquipmentPhotos.Delete;
using MatosKC.Application.EquipmentPhotos.Download;
using MatosKC.Application.EquipmentPhotos.List;
using MatosKC.Application.EquipmentPhotos.Replace;
using MatosKC.Application.EquipmentPhotos.Upload;

using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services
    )
    {
        services.AddScoped<CreateEquipmentCategoryUseCase>();
        services.AddScoped<GetEquipmentCategoryUseCase>();
        services.AddScoped<ListEquipmentCategoryUseCase>();

        services.AddScoped<CreateEquipmentUseCase>();
        services.AddScoped<GetEquipmentUseCase>();
        services.AddScoped<ListEquipmentUseCase>();

        services.AddScoped<UploadEquipmentPhotoUseCase>();
        services.AddScoped<ListEquipmentPhotosUseCase>();
        services.AddScoped<DownloadEquipmentPhotoUseCase>();
        services.AddScoped<ReplaceEquipmentPhotoUseCase>();
        services.AddScoped<DeleteEquipmentPhotoUseCase>();

        return services;
    }
}
