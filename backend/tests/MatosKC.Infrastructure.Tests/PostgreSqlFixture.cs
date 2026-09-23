namespace MatosKC.Infrastructure.Tests;

using MatosKC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer PostgreSqlContainer =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("matoskc_tests")
            .WithUsername("matoskc")
            .WithPassword("matoskc_tests")
            .Build();

    public async ValueTask InitializeAsync()
    {
        await PostgreSqlContainer.StartAsync();

        await using MatosKCDbContext dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await PostgreSqlContainer.DisposeAsync();
    }

    public MatosKCDbContext CreateDbContext()
    {
        DbContextOptions<MatosKCDbContext> options =
            new DbContextOptionsBuilder<MatosKCDbContext>()
                .UseNpgsql(PostgreSqlContainer.GetConnectionString())
                .Options;

        return new MatosKCDbContext(options);
    }
}
