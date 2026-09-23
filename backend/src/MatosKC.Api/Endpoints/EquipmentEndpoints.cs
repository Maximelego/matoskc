namespace MatosKC.Api.Endpoints;

using MatosKC.Api.Contracts;
using MatosKC.Application.Equipments.Create;
using Microsoft.AspNetCore.Http.HttpResults;

public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/equipments")
            .WithTags("Equipment");

        group.MapPost("/", CreateAsync)
            .WithName("CreateEquipment")
            .WithSummary("Create an equipment")
            .Produces<CreatedResourceResponse>(
                StatusCodes.Status201Created
            )
            .ProducesProblem(
                StatusCodes.Status400BadRequest
            )
            .ProducesProblem(
                StatusCodes.Status409Conflict
            );

        return endpoints;
    }

    private static async Task<
        Created<CreatedResourceResponse>
    > CreateAsync(
        CreateEquipmentDto dto,
        CreateEquipmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid equipmentId = await useCase.ExecuteAsync(
            dto,
            cancellationToken
        );

        var response =
            new CreatedResourceResponse(equipmentId);

        return TypedResults.Created(
            $"/api/equipments/{equipmentId}",
            response
        );
    }
}
