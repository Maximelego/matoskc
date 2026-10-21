namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Domain.Entities.Maintenance;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class EquipmentDefectConfiguration : IEntityTypeConfiguration<EquipmentDefect>
{
    public void Configure(EntityTypeBuilder<EquipmentDefect> builder)
    {
        builder.ToTable("equipment_defects", table => table.HasCheckConstraint("ck_defect_source",
            "num_nonnulls(inspection_answer_id, inspection_finding_id) = 1"));
        builder.Property(defect => defect.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(defect => defect.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InspectionEntity>().WithMany().HasForeignKey(defect => defect.InspectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.OperatorFirstName).IsRequired();
        builder.HasOne<Account>().WithMany().HasForeignKey(value => value.ReportedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InspectionAnswer>().WithMany().HasForeignKey(defect => defect.InspectionAnswerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EquipmentMaintenance>().WithMany().HasForeignKey(defect => defect.ResolvedByMaintenanceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(defect => defect.ResolvedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(defect => defect.InspectionAnswerId).IsUnique();
        builder.HasOne<InspectionFinding>().WithMany().HasForeignKey(value => value.InspectionFindingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.InspectionFindingId).IsUnique();
        builder.HasIndex(defect => new { defect.EquipmentId, defect.Status, defect.ReportedAt });
        InspectionColumns.Apply(builder);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

public sealed class DefectOccurrenceConfiguration : IEntityTypeConfiguration<DefectOccurrence>
{
    public void Configure(EntityTypeBuilder<DefectOccurrence> builder)
    {
        builder.ToTable("equipment_defect_occurrences", table => table.HasCheckConstraint("ck_occurrence_source",
            "num_nonnulls(inspection_answer_id, inspection_finding_id, inspection_defect_review_id) = 1"));
        builder.Ignore(occurrence => occurrence.PhotoIds);
        builder.Property<List<Guid>>("_photoIds").HasColumnName("photo_ids").HasColumnType("uuid[]");
        builder.HasOne<EquipmentDefect>().WithMany().HasForeignKey(occurrence => occurrence.DefectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InspectionEntity>().WithMany().HasForeignKey(occurrence => occurrence.InspectionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.OperatorFirstName).IsRequired();
        builder.HasOne<Account>().WithMany().HasForeignKey(value => value.ReportedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InspectionAnswer>().WithMany().HasForeignKey(occurrence => occurrence.InspectionAnswerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(occurrence => occurrence.InspectionAnswerId).IsUnique();
        builder.HasOne<InspectionFinding>().WithMany().HasForeignKey(value => value.InspectionFindingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InspectionDefectReview>().WithMany().HasForeignKey(value => value.InspectionDefectReviewId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.InspectionFindingId).IsUnique();
        builder.HasIndex(value => value.InspectionDefectReviewId).IsUnique();
        InspectionColumns.Apply(builder);
        builder.Property<List<Guid>>("_photoIds").HasColumnName("photo_ids");
    }
}

public sealed class EquipmentMaintenanceConfiguration : IEntityTypeConfiguration<EquipmentMaintenance>
{
    public void Configure(EntityTypeBuilder<EquipmentMaintenance> builder)
    {
        builder.ToTable("equipment_maintenances");
        builder.Property(maintenance => maintenance.PerformedBy).HasMaxLength(200);
        builder.Property(maintenance => maintenance.WorkDescription).HasMaxLength(10000);
        builder.Ignore(maintenance => maintenance.ResolvedDefectIds);
        builder.Property<List<Guid>>("_resolvedDefectIds").HasColumnName("resolved_defect_ids").HasColumnType("uuid[]");
        builder.HasOne<Equipment>().WithMany().HasForeignKey(maintenance => maintenance.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(maintenance => maintenance.RecordedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(maintenance => new { maintenance.EquipmentId, maintenance.PerformedAt });
        InspectionColumns.Apply(builder);
        builder.Property<List<Guid>>("_resolvedDefectIds").HasColumnName("resolved_defect_ids");
    }
}
