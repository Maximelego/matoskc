using Microsoft.Extensions.DependencyInjection;
namespace MatosKC.Application;

using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using MatosKC.Application.EquipmentCategories.List;
using MatosKC.Application.EquipmentPhotos.Delete;
using MatosKC.Application.EquipmentPhotos.Download;
using MatosKC.Application.EquipmentPhotos.List;
using MatosKC.Application.EquipmentPhotos.Replace;
using MatosKC.Application.EquipmentPhotos.Upload;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Get;
using MatosKC.Application.Equipments.List;

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

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<MatosKC.Application.Accounts.Create.CreateAccountUseCase>();
        services.AddScoped<MatosKC.Application.Accounts.Get.GetAccountUseCase>();
        services.AddScoped<MatosKC.Application.Accounts.List.ListAccountsUseCase>();
        services.AddScoped<MatosKC.Application.Accounts.Update.UpdateAccountUseCase>();
        services.AddScoped<MatosKC.Application.Accounts.Delete.DeleteAccountUseCase>();
        services.AddScoped<MatosKC.Application.Agencies.Manage.ManageAgenciesUseCase>();
        services.AddScoped<MatosKC.Application.Auth.VerifySession.VerifyAuthenticationSessionUseCase>();
        services.AddScoped<MatosKC.Application.Auth.RevokeSession.RevokeAuthenticationSessionUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.InspectionWorkflowContext>();
        services.AddScoped<MatosKC.Application.Inspections.CreateDepartureInspectionUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.CreateReturnInspectionUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.SaveInspectionDraftUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.ValidateDepartureInspectionUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.ValidateReturnInspectionUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.QueryInspectionsUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.ManageInspectionTemplatesUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.ManageInspectionPhotosUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.ChangeEquipmentStatusUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.SignInspectionUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.DownloadInspectionSignatureUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.DownloadInspectionReportUseCase>();

        services.AddScoped<MatosKC.Application.Inspections.SearchInspectionsUseCase>();
        services.AddScoped<MatosKC.Application.Inspections.CancelInspectionUseCase>();

        services.AddScoped<MatosKC.Application.Inspections.ManageInspectionTemplatePhotosUseCase>();

        return services;
    }
}
