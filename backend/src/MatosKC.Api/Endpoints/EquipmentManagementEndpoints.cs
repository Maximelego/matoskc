namespace MatosKC.Api.Endpoints;

using MatosKC.Api.Authentication;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Equipments;
using MatosKC.Application.Equipments.Mappings;

public static class EquipmentManagementEndpoints
{
    public static void MapEquipmentManagementEndpoints(this WebApplication app)
    {
        app.MapGet("/equipments/{id:guid}/history", async (Guid id, int? offset, int? limit,
            GetEquipmentHistoryUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(id, offset ?? 0, limit ?? 50,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct)))
            .RequireAuthorization("ManageEquipment");

        app.MapPut("/equipments/{id:guid}", async (Guid id, UpdateEquipmentRequest request,
            ManageEquipmentUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
        {
            var equipment = await useCase.UpdateAsync(id, request,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct);

            return Results.Ok(equipment.ToDto());
        })
        .WithMetadata(new RequireCsrfValidation())
        .RequireAuthorization("ManageEquipment");

        app.MapPut("/equipment-categories/{id:guid}", async (Guid id, UpdateCategoryRequest request,
            ManageEquipmentUseCase useCase, HttpContext http, IAgencyRepository agencies, CancellationToken ct) =>
        {
            var category = await useCase.UpdateCategoryAsync(id, request,
                await InspectionEndpoints.ActorAsync(http, agencies, ct), ct);

            return Results.Ok(new { category.Id, category.Name, category.Description, category.AgencyId });
        })
        .WithMetadata(new RequireCsrfValidation())
        .RequireAuthorization("ManageEquipment");
    }
}
