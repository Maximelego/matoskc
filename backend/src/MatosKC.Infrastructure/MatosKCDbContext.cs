namespace MatosKC.Infrastructure.Persistence;

using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;

public sealed class MatosKCDbContext : DbContext
{
    public DbSet<Equipment> Equipments => Set<Equipment>();

    public DbSet<EquipmentCategory> EquipmentCategories =>
        Set<EquipmentCategory>();

    public MatosKCDbContext(
        DbContextOptions<MatosKCDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MatosKCDbContext).Assembly
        );
    }
}
