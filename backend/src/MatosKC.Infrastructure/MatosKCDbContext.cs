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

    public DbSet<MatosKC.Domain.Entities.Inspection.InspectionTemplatePhoto> InspectionTemplatePhotos => Set<MatosKC.Domain.Entities.Inspection.InspectionTemplatePhoto>();

    public DbSet<MatosKC.Domain.Entities.Maintenance.EquipmentDefect> EquipmentDefects =>
        Set<MatosKC.Domain.Entities.Maintenance.EquipmentDefect>();

    public DbSet<MatosKC.Domain.Entities.Maintenance.DefectOccurrence> DefectOccurrences =>
        Set<MatosKC.Domain.Entities.Maintenance.DefectOccurrence>();

    public DbSet<MatosKC.Domain.Entities.Maintenance.EquipmentMaintenance> EquipmentMaintenances =>
        Set<MatosKC.Domain.Entities.Maintenance.EquipmentMaintenance>();

    public DbSet<Equipment> Equipments => Set<Equipment>();

    public DbSet<EquipmentPhoto> EquipmentPhotos => Set<EquipmentPhoto>();

    public DbSet<EquipmentCategory> EquipmentCategories =>
        Set<EquipmentCategory>();

    private readonly MatosKC.Application.Persistence.AuditContext? Audit;

    public DbSet<MatosKC.Application.Persistence.AuditEntry> AuditEntries =>
        Set<MatosKC.Application.Persistence.AuditEntry>();

    public MatosKCDbContext(
        DbContextOptions<MatosKCDbContext> options,
        MatosKC.Application.Persistence.AuditContext? audit = null)
        : base(options)
    {
        Audit = audit;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var records = AuditCollector.Collect(this, Audit);
        AuditEntries.AddRange(records);

        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch
        {
            foreach (var record in records) Entry(record).State = EntityState.Detached;
            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var records = AuditCollector.Collect(this, Audit);
        AuditEntries.AddRange(records);

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch
        {
            foreach (var record in records) Entry(record).State = EntityState.Detached;
            throw;
        }
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
