using MatosKC.Api.Authentication;
using MatosKC.Application.Agencies.List;
using MatosKC.Application.Agencies.Manage;
using MatosKC.Application.Agencies.Ports;

namespace MatosKC.Api.Endpoints;

public static class AgencyEndpoints
{
    public static void MapAgencyEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/agencies")
            .WithTags("Agencies")
            .RequireAuthorization("ManageAgencies")
            .WithMetadata(new RequireCsrfValidation());

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
    }

    private static async Task<IResult> ListAsync(
        string? name,
        string? code,
        IAgencyRepository repository,
        CancellationToken cancellationToken
    )
    {
        var query = new ListAgenciesQuery { Name = name, Code = code };
        var agencies = await repository.ListByQueryAsync(query, cancellationToken);

        return Results.Ok(new { agencies });
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ManageAgenciesUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        var agency = await useCase.GetAsync(id, cancellationToken);

        return Results.Ok(agency);
    }

    private static async Task<IResult> CreateAsync(
        AgencyDto dto,
        ManageAgenciesUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        var agency = await useCase.CreateAsync(dto, cancellationToken);

        return Results.Created($"/agencies/{agency.Id}", agency);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        AgencyDto dto,
        ManageAgenciesUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        var agency = await useCase.UpdateAsync(id, dto, cancellationToken);

        return Results.Ok(agency);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ManageAgenciesUseCase useCase,
        CancellationToken cancellationToken
    )
    {
        await useCase.DeleteAsync(id, cancellationToken);

        return Results.NoContent();
    }
}
