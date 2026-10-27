namespace MatosKC.Api.Endpoints;

public static class HealthEndpoint
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
            .WithTags("Health")
            .AllowAnonymous();
    }
}