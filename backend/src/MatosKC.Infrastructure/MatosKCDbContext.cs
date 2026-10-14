using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence;

using MatosKC.Domain.Equipments;

public sealed class MatosKCDbContext : DbContext
{
    public DbSet<MatosKC.Domain.Entities.Accounts.Account> Accounts => Set<MatosKC.Domain.Entities.Accounts.Account>();
    public DbSet<MatosKC.Domain.Entities.Agencies.Agency> Agencies => Set<MatosKC.Domain.Entities.Agencies.Agency>();
    public DbSet<MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession> AuthenticationSessions => Set<MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession>();

    public DbSet<MatosKC.Domain.Entities.Clients.Client> Clients => Set<MatosKC.Domain.Entities.Clients.Client>();
    public DbSet<MatosKC.Domain.Entities.Rental.Rental> Rentals => Set<MatosKC.Domain.Entities.Rental.Rental>();
    public DbSet<MatosKC.Domain.Entities.Inspection.Inspection> Inspections => Set<MatosKC.Domain.Entities.Inspection.Inspection>();
    public DbSet<MatosKC.Domain.Entities.Inspection.InspectionTemplate> InspectionTemplates => Set<MatosKC.Domain.Entities.Inspection.InspectionTemplate>();
    public DbSet<MatosKC.Domain.Entities.Inspection.InspectionMedia> InspectionMedia => Set<MatosKC.Domain.Entities.Inspection.InspectionMedia>();

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
