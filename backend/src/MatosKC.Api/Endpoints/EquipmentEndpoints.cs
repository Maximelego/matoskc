namespace MatosKC.Api.Endpoints;

using MatosKC.Api.Contracts;
using MatosKC.Application.Equipments.Create;
using MatosKC.Application.Equipments.Get;
using MatosKC.Application.Equipments.List;
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

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetEquipmentById")
            .WithSummary("Get an equipment by id")
            .Produces<GetEquipmentDto>(
                StatusCodes.Status200OK
            )
            .ProducesProblem(
                StatusCodes.Status404NotFound
            );

        group.MapGet("/", ListEquipmentAsync)
            .WithName("GetEquipmentsByFilter")
            .WithSummary("Get equipments by filters")
            .Produces<ListEquipmentResult>(
                StatusCodes.Status200OK
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

    private static async Task<
        Ok<GetEquipmentDto>
    > GetByIdAsync(
        Guid id,
        GetEquipmentUseCase useCase,
        CancellationToken cancellationToken)
    {
        GetEquipmentDto equipment = await useCase.ExecuteAsync(
            id,
            cancellationToken
        );

        return TypedResults.Ok(equipment);
    }

    private static async Task<
        Ok<ListEquipmentResult>
    > ListEquipmentAsync(
        ListEquipmentsQuery query,
        ListEquipmentUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        ListEquipmentResult result = await useCase.ExecuteAsync(
            query,
            cancellationToken
        );

        return TypedResults.Ok(result);
    }
}
