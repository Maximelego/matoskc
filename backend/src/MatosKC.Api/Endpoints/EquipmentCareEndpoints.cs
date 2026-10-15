namespace MatosKC.Api.Endpoints;

using MatosKC.Api.Authentication;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Maintenance;

public static class EquipmentCareEndpoints
{
    public static void MapEquipmentCareEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/equipments/{equipmentId:guid}")
            .WithTags("Equipment defects and maintenance")
            .WithMetadata(new RequireCsrfValidation())
            .RequireAuthorization();

        group.MapGet("/defects", async (Guid equipmentId, EquipmentDefectStatus? status, int? offset, int? limit,
            QueryEquipmentCareUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
            Results.Ok(await useCase.ListDefectsAsync(equipmentId, status, offset ?? 0, limit ?? 50,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct)));

        group.MapGet("/maintenances", async (Guid equipmentId, int? offset, int? limit,
            QueryEquipmentCareUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
            Results.Ok(await useCase.ListMaintenancesAsync(equipmentId, offset ?? 0, limit ?? 50,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct)));

        group.MapGet("/maintenances/{maintenanceId:guid}", async (Guid equipmentId, Guid maintenanceId,
            QueryEquipmentCareUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
            Results.Ok(await useCase.GetMaintenanceAsync(equipmentId, maintenanceId,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct)));

        group.MapPost("/maintenances", async (Guid equipmentId, RecordMaintenanceRequest request,
            RecordMaintenanceUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
        {
            var maintenance = await useCase.ExecuteAsync(equipmentId, request,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct);

            return Results.Created($"/equipments/{equipmentId}/maintenances/{maintenance.Id}", maintenance);
        })
        .RequireAuthorization("ManageEquipment");

        // This route shares equipment condition photos, never rental/client data.
        group.MapGet("/defects/{defectId:guid}/photos/{photoId:guid}", async (
            Guid equipmentId, Guid defectId, Guid photoId, QueryEquipmentCareUseCase useCase,
            HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
        {
            var image = await useCase.DownloadDefectPhotoAsync(equipmentId, defectId, photoId,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct);
            http.Response.Headers.CacheControl = "private, no-store";

            return Results.Stream(image.Content, image.ContentType);
        });
    }
}
