using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Domain.Equipments;

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

        builder.Property(category => category.AgencyId).HasColumnName("agency_id");
        builder.HasOne<MatosKC.Domain.Entities.Agencies.Agency>().WithMany().HasForeignKey(value => value.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(value => new { value.AgencyId, value.Id });
        builder.HasIndex(category => new { category.AgencyId, category.Name })
            .IsUnique()
            .HasDatabaseName(
                "ux_equipment_categories_name"
            );
    }
}
