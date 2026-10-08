using MatosKC.Api.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MatosKC.Api.Tests.Endpoints;
// Tests of the real authentication middleware, requiring neither Docker nor a database.
public sealed class SecurityPipelineTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:MatosKCDatabase",
                    "Host=localhost;Database=unused;Username=unused;Password=unused");
                builder.UseSetting("S3:ServiceUrl", "http://unused.test");
                builder.UseSetting("S3:Region", "us-east-1");
                builder.UseSetting("S3:AccessKey", "test");
                builder.UseSetting("S3:SecretKey", "test");
                builder.UseSetting("S3:BucketName", "test");
            });

    [Fact]
    public async Task RegisteredEndpoints_UseUnprefixedRoutesAndRequireCsrfForMutations()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Inspect actual routing metadata: an anonymous 401 alone can hide a missing route.
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var endpoints = source.Endpoints.OfType<RouteEndpoint>().ToArray();

        Assert.DoesNotContain(endpoints,
            endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api") == true);

        string[] expectedRoutes =
        [
            "/auth/login",
            "/auth/logout",
            "/auth/me",
            "/accounts/",
            "/agencies/",
            "/equipments",
            "/equipment-categories",
            "/equipments/{equipmentId:guid}/photos"
        ];

        foreach (string expected in expectedRoutes)
        {
            Assert.Contains(endpoints, endpoint =>
                endpoint.RoutePattern.RawText?.TrimEnd('/') == expected.TrimEnd('/'));
        }

        foreach (var endpoint in endpoints)
        {
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;

            if (methods?.Any(method => method is "POST" or "PUT" or "PATCH" or "DELETE") == true)
            {
                Assert.NotNull(endpoint.Metadata.GetMetadata<RequireCsrfValidation>());
            }
        }
    }

    [Theory]
    [InlineData("/auth/me")]
    [InlineData("/accounts")]
    [InlineData("/agencies")]
    [InlineData("/equipments")]
    public async Task ProtectedEndpoints_RequireAuthentication(string path)
    {
        await using var factory = CreateFactory();
        using var client =
            factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync(path, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task CsrfCookie_UsesSecureHttpOnlyHostPrefix()
    {
        await using var factory = CreateFactory();
        using var client =
            factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/auth/csrf", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("__Host-MatosKC.Csrf=", cookie);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        using var body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("X-CSRF-TOKEN", body.RootElement.GetProperty("headerName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_RequiresCsrfBeforeCredentialChecks()
    {
        await using var factory = CreateFactory();
        using var client =
            factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(
                 "/auth/login", new { password = "" }, TestContext.Current.CancellationToken))
                .StatusCode);
        await MatosKCApiFactory.RefreshCsrfAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync(
                 "/auth/login", new { password = "" }, TestContext.Current.CancellationToken))
                .StatusCode);
    }

    [Fact]
    public async Task Login_IsRateLimited()
    {
        await using var factory = CreateFactory();
        using var client =
            factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await MatosKCApiFactory.RefreshCsrfAsync(client);
        for (int i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.PostAsJsonAsync(
                     "/auth/login", new { password = "" }, TestContext.Current.CancellationToken))
                    .StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync(
                 "/auth/login", new { password = "" }, TestContext.Current.CancellationToken))
                .StatusCode);
    }

    [Fact]
    public async Task FabricatedCookie_DoesNotAuthenticate()
    {
        await using var factory = CreateFactory();
        using var client =
            factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Cookie", "__Host-MatosKC.Session=forged");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/auth/me", TestContext.Current.CancellationToken)).StatusCode);
    }
}
