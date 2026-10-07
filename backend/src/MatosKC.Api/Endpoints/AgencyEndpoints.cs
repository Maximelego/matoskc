using MatosKC.Application.Agencies.List;
using MatosKC.Application.Agencies.Manage;
using MatosKC.Application.Agencies.Ports;
namespace MatosKC.Api.Endpoints;
public static class AgencyEndpoints
{
    public static void MapAgencyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/agencies").WithTags("Agencies").RequireAuthorization("ManageAgencies");
        group.MapGet("/", async (string? name, string? code, IAgencyRepository repository, CancellationToken ct) =>
            Results.Ok(new { agencies = await repository.ListByQueryAsync(new ListAgenciesQuery { Name = name, Code = code }, ct) }));
        group.MapGet("/{id:guid}", async (Guid id, ManageAgenciesUseCase useCase, CancellationToken ct) => Results.Ok(await useCase.GetAsync(id, ct)));
        group.MapPost("/", async (AgencyDto dto, ManageAgenciesUseCase useCase, CancellationToken ct) =>
        {
            var agency = await useCase.CreateAsync(dto, ct);
            return Results.Created($"/api/agencies/{agency.Id}", agency);
        });
        group.MapPut("/{id:guid}", async (Guid id, AgencyDto dto, ManageAgenciesUseCase useCase, CancellationToken ct) => Results.Ok(await useCase.UpdateAsync(id, dto, ct)));
        group.MapDelete("/{id:guid}", async (Guid id, ManageAgenciesUseCase useCase, CancellationToken ct) =>
        { await useCase.DeleteAsync(id, ct); return Results.NoContent(); });
    }
}
