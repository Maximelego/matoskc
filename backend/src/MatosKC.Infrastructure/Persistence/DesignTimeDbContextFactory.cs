namespace MatosKC.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MatosKCDbContext>
{
    public MatosKCDbContext CreateDbContext(string[] args)
    {
        string connection = Environment.GetEnvironmentVariable("ConnectionStrings__MatosKCDatabase")
            ?? throw new InvalidOperationException("Set ConnectionStrings__MatosKCDatabase for EF tooling.");

        return new MatosKCDbContext(new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql(connection).Options);
    }
}
