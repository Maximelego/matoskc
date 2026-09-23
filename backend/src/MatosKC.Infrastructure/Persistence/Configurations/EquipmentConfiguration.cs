namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class EquipmentConfiguration
    : IEntityTypeConfiguration<Equipment>
{
    public void Configure(
        EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipments");

        builder.HasKey(equipment => equipment.Id);

        builder.Property(equipment => equipment.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(equipment => equipment.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(equipment => equipment.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(equipment => equipment.SerialNumber)
            .HasColumnName("serial_number")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(equipment => equipment.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(equipment => equipment.SerialNumber)
            .IsUnique()
            .HasDatabaseName(
                "ux_equipments_serial_number"
            );

        builder.HasIndex(equipment => equipment.CategoryId)
            .HasDatabaseName(
                "ix_equipments_category_id"
            );

        builder.HasOne<EquipmentCategory>()
            .WithMany()
            .HasForeignKey(equipment => equipment.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
