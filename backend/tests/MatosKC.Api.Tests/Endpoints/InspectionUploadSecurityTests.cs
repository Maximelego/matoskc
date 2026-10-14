namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class InspectionUploadSecurityTests
{
    private static WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:MatosKCDatabase", "Host=localhost;Database=unused;Username=unused;Password=unused");
            builder.UseSetting("S3:ServiceUrl", "http://unused.test");
            builder.UseSetting("S3:Region", "us-east-1");
            builder.UseSetting("S3:AccessKey", "test");
            builder.UseSetting("S3:SecretKey", "test");
            builder.UseSetting("S3:BucketName", "test");
            builder.ConfigureServices(services => services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestInspector";
                options.DefaultChallengeScheme = "TestInspector";
            }).AddScheme<AuthenticationSchemeOptions, TestInspector>("TestInspector", _ => { }));
        });

    [Theory]
    [InlineData("/equipments/11111111-1111-1111-1111-111111111111/photos")]
    [InlineData("/inspections/11111111-1111-1111-1111-111111111111/answers/22222222-2222-2222-2222-222222222222/photos")]
    public async Task MultipartUploadWithoutCsrf_IsRejectedBeforeDatabaseAccess(string path)
    {
        await using var factory = Factory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new("image/png");
        form.Add(file, "file", "photo.png");

        var response = await client.PostAsync(path, form, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Invalid CSRF token", body.RootElement.GetProperty("title").GetString());
    }

    private sealed class TestInspector(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "33333333-3333-3333-3333-333333333333"),
                new Claim(ClaimTypes.Name, "Test inspector"),
                new Claim(ClaimTypes.Role, "SuperAdmin")
            ], Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
