namespace MatosKC.Api.Endpoints;

using MatosKC.Api.Authentication;

using MatosKC.Application.EquipmentPhotos;
using MatosKC.Application.EquipmentPhotos.Delete;
using MatosKC.Application.EquipmentPhotos.Download;
using MatosKC.Application.EquipmentPhotos.List;
using MatosKC.Application.EquipmentPhotos.Replace;
using MatosKC.Application.EquipmentPhotos.Upload;

public static class EquipmentPhotoEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentPhotoEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/equipments/{equipmentId:guid}/photos")
            .WithMetadata(new RequireCsrfValidation())
            .WithTags("Equipment photos");

        group.MapPost("", UploadAsync)
            .WithName("UploadEquipmentPhoto")
            .WithSummary("Upload a photo for an equipment")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<EquipmentPhotoResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            // Header validation is handled by CsrfMiddleware, including uploads.
            .DisableAntiforgery();

        group.MapGet("", ListAsync)
            .WithName("ListEquipmentPhotos")
            .WithSummary("List the photos of an equipment")
            .Produces<IReadOnlyList<EquipmentPhotoResult>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{photoId:guid}", DownloadAsync)
            .WithName("DownloadEquipmentPhoto")
            .WithSummary("Download an equipment photo")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{photoId:guid}", ReplaceAsync)
            .WithName("ReplaceEquipmentPhoto")
            .WithSummary("Replace an equipment photo")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<EquipmentPhotoResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            // Header validation is handled by CsrfMiddleware, including uploads.
            .DisableAntiforgery();

        group.MapDelete("/{photoId:guid}", DeleteAsync)
            .WithName("DeleteEquipmentPhoto")
            .WithSummary("Delete an equipment photo")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> UploadAsync(
        Guid equipmentId,
        IFormFile file,
        UploadEquipmentPhotoUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        await using Stream content = file.OpenReadStream();
        EquipmentPhotoResult result = await useCase.ExecuteAsync(
            equipmentId,
            content,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken
        );

        return Results.Created(
            $"/equipments/{equipmentId}/photos/{result.Id}",
            result
        );
    }

    private static async Task<IResult> ListAsync(
        Guid equipmentId,
        ListEquipmentPhotosUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<EquipmentPhotoResult> result =
            await useCase.ExecuteAsync(equipmentId, cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> DownloadAsync(
        Guid equipmentId,
        Guid photoId,
        DownloadEquipmentPhotoUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        DownloadEquipmentPhotoResult result = await useCase.ExecuteAsync(
            equipmentId,
            photoId,
            cancellationToken
        );

        return Results.Stream(
            result.Object.Content,
            result.Object.ContentType,
            result.FileName,
            enableRangeProcessing: true
        );
    }

    private static async Task<IResult> ReplaceAsync(
        Guid equipmentId,
        Guid photoId,
        IFormFile file,
        ReplaceEquipmentPhotoUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        await using Stream content = file.OpenReadStream();
        EquipmentPhotoResult result = await useCase.ExecuteAsync(
            equipmentId,
            photoId,
            content,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken
        );

        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(
        Guid equipmentId,
        Guid photoId,
        DeleteEquipmentPhotoUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        await useCase.ExecuteAsync(equipmentId, photoId, cancellationToken);
        return Results.NoContent();
    }
}
