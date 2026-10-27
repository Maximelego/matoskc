namespace MatosKC.Api.ErrorHandling;

using System.Text.Json;
using MatosKC.Application.EquipmentCategories.Create.Exceptions;
using MatosKC.Application.EquipmentCategories.Get.Exceptions;
using MatosKC.Application.EquipmentPhotos.Exceptions;
using MatosKC.Application.Equipments.Create.Exceptions;
using MatosKC.Application.Equipments.Get.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int statusCode, string title, string detail) =
            exception switch
            {
                MatosKC.Application.Administration.AdministrationException administration =>
                    (administration.StatusCode, "Cannot complete operation", administration.Message),

                MatosKC.Application.Inspections.InspectionOperationException operation =>
                    (operation.StatusCode, "Cannot complete inspection operation", operation.Message),

                BadHttpRequestException request =>
                    (
                        request.StatusCode,
                        "Invalid request",
                        "The request body is malformed or contains a value with an invalid type."
                    ),

                JsonException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Invalid JSON",
                        "The JSON body could not be parsed."
                    ),

                ArgumentException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Invalid request",
                        exception.Message
                    ),

                EquipmentCategoryNotFoundException =>
                    (
                        StatusCodes.Status404NotFound,
                        "Equipment category not found",
                        exception.Message
                    ),

                EquipmentNotFoundException =>
                    (
                        StatusCodes.Status404NotFound,
                        "Equipment not found",
                        exception.Message
                    ),

                EquipmentPhotoNotFoundException =>
                    (
                        StatusCodes.Status404NotFound,
                        "Equipment photo not found",
                        exception.Message
                    ),

                EquipmentCategoryAlreadyExistsException =>
                    (
                        StatusCodes.Status409Conflict,
                        "Equipment category already exists",
                        exception.Message
                    ),

                EquipmentSerialNumberAlreadyExistsException =>
                    (
                        StatusCodes.Status409Conflict,
                        "Equipment serial number already exists",
                        exception.Message
                    ),

                _ =>
                    (
                        StatusCodes.Status500InternalServerError,
                        "Internal server error",
                        "An unexpected error occurred."
                    )
            };

        if (statusCode ==
            StatusCodes.Status500InternalServerError)
        {
            return false;
        }

        await Results.Problem(
            detail: detail,
            statusCode: statusCode,
            title: title
        ).ExecuteAsync(httpContext);

        return true;
    }
}
