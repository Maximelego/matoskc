namespace MatosKC.Api.Endpoints;

using System.Security.Claims;
using MatosKC.Api.Authentication;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Equipments;

public static class InspectionEndpoints
{
    public static void MapInspectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/inspections")
            .WithTags("Inspections")
            .WithMetadata(new RequireCsrfValidation())
            .RequireAuthorization();

        group.MapPost("/departure", CreateDepartureAsync)
            .RequireAuthorization("PerformInspections");

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);

        group.MapPut("/{id:guid}/draft", SaveDraftAsync)
            .RequireAuthorization("PerformInspections");

        group.MapPost("/{id:guid}/validation", ValidateAsync)
            .RequireAuthorization("PerformInspections");

        group.MapPost("/{id:guid}/answers/{answerId:guid}/photos", UploadPhotoAsync)
            .RequireAuthorization("PerformInspections")
            .DisableAntiforgery();

        group.MapGet("/{id:guid}/photos/{photoId:guid}", DownloadPhotoAsync);
        group.MapDelete("/{id:guid}/photos/{photoId:guid}", RemovePhotoAsync)
            .RequireAuthorization("PerformInspections");

        var rentals = app.MapGroup("/rentals")
            .WithTags("Rentals")
            .WithMetadata(new RequireCsrfValidation())
            .RequireAuthorization();

        rentals.MapGet("/{id:guid}", GetRentalAsync);
        rentals.MapGet("/{id:guid}/departure-inspection", GetDepartureAsync);
        rentals.MapPost("/{id:guid}/return-inspection", CreateReturnAsync)
            .RequireAuthorization("PerformInspections");

        var templates = app.MapGroup("/inspection-templates")
            .WithTags("Inspection templates")
            .WithMetadata(new RequireCsrfValidation())
            .RequireAuthorization();

        templates.MapGet("/{id:guid}", async (Guid id, ManageInspectionTemplatesUseCase useCase, CancellationToken ct) =>
            Results.Ok(await useCase.GetAsync(id, ct)));

        templates.MapPost("/", CreateTemplateAsync)
            .RequireAuthorization("ManageEquipment");

        app.MapGet("/equipment-categories/{categoryId:guid}/inspection-template",
            async (Guid categoryId, ManageInspectionTemplatesUseCase useCase, CancellationToken ct) =>
                Results.Ok(await useCase.GetLatestAsync(categoryId, ct)))
            .WithTags("Inspection templates")
            .RequireAuthorization();

        app.MapGet("/equipments/{equipmentId:guid}/current-rental", GetOpenRentalAsync)
            .WithTags("Rentals")
            .RequireAuthorization();

        app.MapPatch("/equipments/{equipmentId:guid}/status", ChangeStatusAsync)
            .WithTags("Equipment")
            .WithMetadata(new RequireCsrfValidation())
            .RequireAuthorization("ManageEquipment");
    }

    private static async Task<InspectionActor> ActorAsync(HttpContext context, IAgencyRepository agencies, CancellationToken ct)
    {
        Guid accountId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Role role = Enum.Parse<Role>(context.User.FindFirstValue(ClaimTypes.Role)!);
        int? code = null;
        Guid? currentAgencyId = null;

        if (Guid.TryParse(context.User.FindFirstValue("agency_id"), out Guid agencyId))
        {
            currentAgencyId = agencyId;
            code = (await agencies.GetByIdAsync(agencyId, ct))?.Code;
        }

        return new InspectionActor(accountId, role, code, currentAgencyId);
    }

    private static async Task<IResult> CreateDepartureAsync(
        CreateDepartureRequest request,
        CreateDepartureInspectionUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var inspection = await useCase.ExecuteAsync(request, await ActorAsync(context, agencies, ct), ct);

        return Results.Created($"/inspections/{inspection.Id}", inspection);
    }

    private static async Task<IResult> CreateReturnAsync(
        Guid id,
        CreateReturnRequest request,
        CreateReturnInspectionUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var inspection = await useCase.ExecuteAsync(id, request, await ActorAsync(context, agencies, ct), ct);

        return Results.Ok(inspection);
    }

    private static async Task<IResult> SaveDraftAsync(
        Guid id,
        SaveDraftRequest request,
        SaveInspectionDraftUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    ) => Results.Ok(await useCase.ExecuteAsync(id, request, await ActorAsync(context, agencies, ct), ct));

    private static async Task<IResult> ValidateAsync(
        Guid id,
        InspectionWorkflowContext workflow,
        ValidateDepartureInspectionUseCase departure,
        ValidateReturnInspectionUseCase returnUseCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var actor = await ActorAsync(context, agencies, ct);
        var (inspection, _) = await workflow.GetInspectionAsync(id, actor, ct);
        var result = inspection.Type == InspectionType.Departure
            ? await departure.ExecuteAsync(id, actor, ct)
            : await returnUseCase.ExecuteAsync(id, actor, ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetAsync(
        Guid id, QueryInspectionsUseCase useCase, HttpContext context, IAgencyRepository agencies, CancellationToken ct
    ) => Results.Ok(await useCase.GetAsync(id, await ActorAsync(context, agencies, ct), ct));

    private static async Task<IResult> ListAsync(
        Guid? rentalId,
        InspectionType? type,
        InspectionStatus? status,
        int? offset,
        int? limit,
        QueryInspectionsUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var inspections = await useCase.ListAsync(await ActorAsync(context, agencies, ct),
            rentalId, type, status, offset ?? 0, limit ?? 50, ct);

        return Results.Ok(new { inspections });
    }

    private static async Task<IResult> GetRentalAsync(
        Guid id, QueryInspectionsUseCase useCase, HttpContext context, IAgencyRepository agencies, CancellationToken ct
    ) => Results.Ok(await useCase.GetRentalAsync(id, await ActorAsync(context, agencies, ct), ct));

    private static async Task<IResult> GetOpenRentalAsync(
        Guid equipmentId, QueryInspectionsUseCase useCase, HttpContext context, IAgencyRepository agencies, CancellationToken ct
    ) => Results.Ok(await useCase.GetOpenRentalAsync(equipmentId, await ActorAsync(context, agencies, ct), ct));

    private static async Task<IResult> GetDepartureAsync(
        Guid id, QueryInspectionsUseCase useCase, HttpContext context, IAgencyRepository agencies, CancellationToken ct
    ) => Results.Ok(await useCase.GetDepartureAsync(id, await ActorAsync(context, agencies, ct), ct));

    private static async Task<IResult> CreateTemplateAsync(
        CreateTemplateRequest request,
        ManageInspectionTemplatesUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var template = await useCase.CreateAsync(request, await ActorAsync(context, agencies, ct), ct);

        return Results.Created($"/inspection-templates/{template.Id}", template);
    }

    private static async Task<IResult> UploadPhotoAsync(
        Guid id,
        Guid answerId,
        IFormFile file,
        ManageInspectionPhotosUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        await using var stream = file.OpenReadStream();
        var media = await useCase.UploadAsync(id, answerId, stream, file.FileName,
            file.ContentType, file.Length, await ActorAsync(context, agencies, ct), ct);

        return Results.Created($"/inspections/{id}/photos/{media.Id}", new
        {
            media.Id, media.InspectionId, media.AnswerId, media.FileName, media.ContentType, media.ContentLength
        });
    }

    private static async Task<IResult> DownloadPhotoAsync(
        Guid id,
        Guid photoId,
        ManageInspectionPhotosUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var content = await useCase.DownloadAsync(id, photoId, await ActorAsync(context, agencies, ct), ct);

        return Results.Stream(content.Content, content.ContentType);
    }

    private static async Task<IResult> RemovePhotoAsync(
        Guid id,
        Guid photoId,
        ManageInspectionPhotosUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        await useCase.RemoveAsync(id, photoId, await ActorAsync(context, agencies, ct), ct);

        return Results.NoContent();
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid equipmentId,
        ChangeEquipmentStatusRequest request,
        ChangeEquipmentStatusUseCase useCase,
        HttpContext context,
        IAgencyRepository agencies,
        CancellationToken ct
    )
    {
        var equipment = await useCase.ExecuteAsync(equipmentId, request.Status, await ActorAsync(context, agencies, ct), ct);

        return Results.Ok(new
        {
            equipment.Id, equipment.Name, equipment.SerialNumber,
            equipmentCategoryId = equipment.CategoryId, equipment.Status
        });
    }

    public sealed record ChangeEquipmentStatusRequest(
        [property: System.Text.Json.Serialization.JsonRequired] EquipmentStatus Status
    );
}
