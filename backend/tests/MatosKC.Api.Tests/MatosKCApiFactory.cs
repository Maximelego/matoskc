namespace MatosKC.Api.Tests;

using MatosKC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

public sealed class MatosKCApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer PostgreSqlContainer =
        new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("matoskc_api_tests")
            .WithUsername("matoskc")
            .WithPassword("matoskc_tests")
            .Build();

    public async ValueTask InitializeAsync()
    {
        await PostgreSqlContainer.StartAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await PostgreSqlContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:MatosKCDatabase"] =
                            PostgreSqlContainer.GetConnectionString()
                    }
                );
            }
        );

        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<MatosKCDbContext>();
                services.RemoveAll<DbContextOptions<MatosKCDbContext>>();

                services.AddDbContext<MatosKCDbContext>(
                    options => options.UseNpgsql(
                        PostgreSqlContainer.GetConnectionString()
                    )
                );

                using ServiceProvider serviceProvider = services.BuildServiceProvider();
                using IServiceScope scope = serviceProvider.CreateScope();
                MatosKCDbContext dbContext =
                    scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();

                dbContext.Database.Migrate();
            }
        );
    }
}
