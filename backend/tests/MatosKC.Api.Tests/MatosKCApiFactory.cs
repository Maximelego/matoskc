namespace MatosKC.Api.Tests;

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

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await PostgreSqlContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        string connectionString =
            PostgreSqlContainer.GetConnectionString();

        // Rend la connexion disponible au Program.cs.
        builder.UseSetting(
            "ConnectionStrings:MatosKCDatabase",
            connectionString
        );

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
            }
        );
    }
}
