namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Domain.Equipments;

public sealed class EquipmentPhotoConfiguration
    : IEntityTypeConfiguration<EquipmentPhoto>
{
    public void Configure(EntityTypeBuilder<EquipmentPhoto> builder)
    {
        builder.ToTable("equipment_photos");
        builder.HasKey(photo => photo.Id);

        builder.Property(photo => photo.Id).HasColumnName("id");
        builder.Property(photo => photo.EquipmentId).HasColumnName("equipment_id");
        builder.Property(photo => photo.ObjectKey).HasColumnName("object_key").HasMaxLength(512);
        builder.Property(photo => photo.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        builder.Property(photo => photo.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(photo => photo.ContentLength).HasColumnName("content_length");
        builder.Property(photo => photo.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(photo => photo.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(photo => photo.EquipmentId)
            .HasDatabaseName("ix_equipment_photos_equipment_id");

        builder.HasIndex(photo => photo.ObjectKey)
            .IsUnique()
            .HasDatabaseName("ux_equipment_photos_object_key");

        builder.HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(photo => photo.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
