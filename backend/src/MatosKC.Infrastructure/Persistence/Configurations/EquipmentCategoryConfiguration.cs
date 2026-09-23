namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class EquipmentCategoryConfiguration
    : IEntityTypeConfiguration<EquipmentCategory>
{
    public void Configure(
        EntityTypeBuilder<EquipmentCategory> builder)
    {
        builder.ToTable("equipment_categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(category => category.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.HasIndex(category => category.Name)
            .IsUnique()
            .HasDatabaseName(
                "ux_equipment_categories_name"
            );
    }
}
