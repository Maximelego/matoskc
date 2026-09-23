namespace MatosKC.Api.Endpoints;

using MatosKC.Api.Contracts;
using MatosKC.Application.EquipmentCategories.Create;
using MatosKC.Application.EquipmentCategories.Get;
using Microsoft.AspNetCore.Http.HttpResults;

public static class EquipmentCategoryEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentCategoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/equipment-categories")
            .WithTags("Equipment categories");

        group.MapPost("/", CreateAsync)
            .WithName("CreateEquipmentCategory")
            .WithSummary("Create an equipment category")
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
            .WithName("GetEquipmentCategoryById")
            .WithSummary("Get an equipment category by identifier")
            .Produces<GetEquipmentCategoryResult>(
                StatusCodes.Status200OK
            )
            .ProducesProblem(
                StatusCodes.Status404NotFound
            );

        return endpoints;
    }

    private static async Task<
        Created<CreatedResourceResponse>
    > CreateAsync(
        CreateEquipmentCategoryDto dto,
        CreateEquipmentCategoryUseCase useCase,
        CancellationToken cancellationToken)
    {
        Guid categoryId = await useCase.ExecuteAsync(
            dto,
            cancellationToken
        );

        var response =
            new CreatedResourceResponse(categoryId);

        return TypedResults.Created(
            $"/api/equipment-categories/{categoryId}",
            response
        );
    }

    private static async Task<
        Ok<GetEquipmentCategoryResult>
    > GetByIdAsync(
        Guid id,
        GetEquipmentCategoryUseCase useCase,
        CancellationToken cancellationToken)
    {
        GetEquipmentCategoryResult result =
            await useCase.ExecuteAsync(
                id,
                cancellationToken
            );

        return TypedResults.Ok(result);
    }
}
