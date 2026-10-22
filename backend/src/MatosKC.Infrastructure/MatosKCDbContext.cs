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
    public MatosKC.Application.Agencies.AgencyScope Scope { get; }

    public DbSet<MatosKC.Application.Persistence.AuditEntry> AuditEntries =>
        Set<MatosKC.Application.Persistence.AuditEntry>();

    public MatosKCDbContext(
        DbContextOptions<MatosKCDbContext> options,
        MatosKC.Application.Persistence.AuditContext? audit = null,
        MatosKC.Application.Agencies.AgencyScope? agencyScope = null)
        : base(options)
    {
        Audit = audit;
        Scope = agencyScope ?? new MatosKC.Application.Agencies.AgencyScope();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareAgencyOwnership();
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
        PrepareAgencyOwnership();
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

    private void PrepareAgencyOwnership()
    {
        ChangeTracker.DetectChanges();
        var owned = ChangeTracker.Entries<MatosKC.Domain.Entities.Agencies.IAgencyOwned>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified).ToArray();
        Guid? legacyAgency = null;
        foreach (var entry in owned)
        {
            // Compatibility for trusted legacy imports/test fixtures. HTTP creation always assigns ownership explicitly.
            if (entry.Entity.AgencyId == Guid.Empty && Scope.IsSystem)
            {
                legacyAgency ??= Agencies.Single(agency => agency.Code == 83).Id;
                entry.Entity.AssignAgency(legacyAgency.Value);
            }
            Scope.EnsureAccess(entry.Entity.AgencyId);
            if (entry.State == EntityState.Modified && !Equals(entry.Property("AgencyId").OriginalValue, entry.Property("AgencyId").CurrentValue))
                throw new MatosKC.Application.Inspections.InspectionOperationException(409, "Agency ownership cannot be transferred.");
        }
        foreach (var entry in ChangeTracker.Entries<MatosKC.Domain.Entities.Rental.Rental>().Where(value => value.State == EntityState.Added))
        {
            if (entry.Entity.AgencyId is null && Scope.IsSystem)
            {
                var equipmentId = entry.Entity.EquipmentId;
                var equipment = Equipments.Local.FirstOrDefault(value => value.Id == equipmentId)
                    ?? Equipments.IgnoreQueryFilters().Single(value => value.Id == equipmentId);
                entry.Property("AgencyId").CurrentValue = equipment.AgencyId;
            }
            Scope.EnsureAccess(entry.Entity.AgencyId ?? Guid.Empty);
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
        modelBuilder.Entity<EquipmentCategory>().HasQueryFilter(value => Scope.CanAccessAll || (Scope.AgencyId != null && value.AgencyId == Scope.AgencyId));
        modelBuilder.Entity<Equipment>().HasQueryFilter(value => Scope.CanAccessAll || (Scope.AgencyId != null && value.AgencyId == Scope.AgencyId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionTemplate>().HasQueryFilter(value => Scope.CanAccessAll || (Scope.AgencyId != null && value.AgencyId == Scope.AgencyId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionTemplatePhoto>().HasQueryFilter(value => Scope.CanAccessAll || (Scope.AgencyId != null && value.AgencyId == Scope.AgencyId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Rental.Rental>().HasQueryFilter(value => Scope.CanAccessAll || (Scope.AgencyId != null && value.AgencyId == Scope.AgencyId));
        modelBuilder.Entity<EquipmentPhoto>().HasQueryFilter(value => Scope.CanAccessAll || Equipments.Any(parent => parent.Id == value.EquipmentId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Maintenance.EquipmentDefect>().HasQueryFilter(value => Scope.CanAccessAll || Equipments.Any(parent => parent.Id == value.EquipmentId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Maintenance.EquipmentMaintenance>().HasQueryFilter(value => Scope.CanAccessAll || Equipments.Any(parent => parent.Id == value.EquipmentId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Maintenance.DefectOccurrence>().HasQueryFilter(value => Scope.CanAccessAll || EquipmentDefects.Any(parent => parent.Id == value.DefectId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.Inspection>().HasQueryFilter(value => Scope.CanAccessAll || Rentals.Any(parent => parent.Id == value.RentalId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionTemplatePoint>().HasQueryFilter(value => Scope.CanAccessAll || InspectionTemplates.Any(parent => parent.Id == value.TemplateId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionAnswer>().HasQueryFilter(value => Scope.CanAccessAll || Inspections.Any(parent => parent.Id == value.InspectionId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionMedia>().HasQueryFilter(value => Scope.CanAccessAll || Inspections.Any(parent => parent.Id == value.InspectionId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionDefectReview>().HasQueryFilter(value => Scope.CanAccessAll || Inspections.Any(parent => parent.Id == value.InspectionId));
        modelBuilder.Entity<MatosKC.Domain.Entities.Inspection.InspectionFinding>().HasQueryFilter(value => Scope.CanAccessAll || Inspections.Any(parent => parent.Id == value.InspectionId));
    }
}
