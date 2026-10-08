namespace MatosKC.Api.Tests;

using MatosKC.Application.Files.Ports;
using MatosKC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

public sealed class MatosKCApiFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer PostgreSqlContainer =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("matoskc_api_tests")
            .WithUsername("matoskc")
            .WithPassword("matoskc_tests")
            .Build();

    public async ValueTask InitializeAsync()
    {
        // 1. Démarre PostgreSQL.
        await PostgreSqlContainer.StartAsync();

        // 2. Force la construction de l'API et de son conteneur DI.
        _ = CreateClient();

        // 3. Applique les migrations avec le vrai conteneur DI de l'API.
        using IServiceScope scope = Services.CreateScope();

        MatosKCDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public HttpClient CreateAuthenticatedClient() => CreateAuthenticatedClientAsync().GetAwaiter().GetResult();

    public async Task<HttpClient> CreateAuthenticatedClientAsync(MatosKC.Domain.Entities.Accounts.Role role = MatosKC.Domain.Entities.Accounts.Role.SuperAdmin)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        string email = $"test-{Guid.NewGuid():N}@example.com";
        const string password = "integration-test-password";
        int? code = null;
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
            Guid? agencyId = null;
            if (role != MatosKC.Domain.Entities.Accounts.Role.SuperAdmin)
            {
                var agency = new MatosKC.Domain.Entities.Agencies.Agency("Test", Random.Shared.Next(100000, int.MaxValue));
                db.Agencies.Add(agency); agencyId = agency.Id; code = agency.Code;
            }
            var hasher = scope.ServiceProvider.GetRequiredService<MatosKC.Application.Accounts.Ports.IPasswordHasher>();
            db.Accounts.Add(new MatosKC.Domain.Entities.Accounts.Account("Test", role == MatosKC.Domain.Entities.Accounts.Role.Agency ? null : email,
                hasher.HashPassword(password), true, role, agencyId));
            await db.SaveChangesAsync();
        }
        await RefreshCsrfAsync(client);
        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/auth/login",
            new
            {
                agencyCode = role == MatosKC.Domain.Entities.Accounts.Role.Agency ? code : null,
                email = role == MatosKC.Domain.Entities.Accounts.Role.Agency ? null : email,
                password
            });
        response.EnsureSuccessStatusCode();
        await RefreshCsrfAsync(client);
        return client;
    }

    public static async Task RefreshCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/csrf"); response.EnsureSuccessStatusCode();
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json.RootElement.GetProperty("token").GetString());
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await PostgreSqlContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Authentication:LoginPermitLimit", "100000");

        string connectionString =
            PostgreSqlContainer.GetConnectionString();

        // Rend la connexion disponible au Program.cs.
        builder.UseSetting(
            "ConnectionStrings:MatosKCDatabase",
            connectionString
        );
        builder.UseSetting("S3:ServiceUrl", "http://unused.test");
        builder.UseSetting("S3:Region", "us-east-1");
        builder.UseSetting("S3:AccessKey", "test");
        builder.UseSetting("S3:SecretKey", "test");
        builder.UseSetting("S3:BucketName", "test");

        builder.ConfigureServices(
            services =>
            {
                // Retire la configuration PostgreSQL enregistrée
                // normalement par Program.cs/AddInfrastructure().
                services.RemoveAll<MatosKCDbContext>();
                services.RemoveAll<DbContextOptions<MatosKCDbContext>>();

                // La remplace par la base PostgreSQL du conteneur de test.
                services.AddDbContext<MatosKCDbContext>(
                    options => options.UseNpgsql(connectionString)
                );

                services.RemoveAll<IObjectStorage>();
                services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
            }
        );
    }
}
