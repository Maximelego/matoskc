using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence;

using MatosKC.Domain.Equipments;

public sealed class MatosKCDbContext : DbContext
{
    public DbSet<MatosKC.Domain.Entities.Accounts.Account> Accounts => Set<MatosKC.Domain.Entities.Accounts.Account>();
    public DbSet<MatosKC.Domain.Entities.Agencies.Agency> Agencies => Set<MatosKC.Domain.Entities.Agencies.Agency>();
    public DbSet<MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession> AuthenticationSessions => Set<MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession>();

    public DbSet<Equipment> Equipments => Set<Equipment>();

    public DbSet<EquipmentPhoto> EquipmentPhotos => Set<EquipmentPhoto>();

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
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MatosKCDbContext).Assembly
        );
    }
}
