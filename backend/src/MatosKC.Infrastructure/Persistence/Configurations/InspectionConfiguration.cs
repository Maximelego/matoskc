namespace MatosKC.Infrastructure.Persistence.Configurations;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Equipments;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

internal static class InspectionColumns
{
    internal static void Apply<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : class
    {
        builder.HasKey("Id");
        builder.Property<Guid>("Id").ValueGeneratedNever();

        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
        }
    }
}

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("rental_clients");
        builder.Property(client => client.DisplayName).IsRequired();
        builder.Property(client => client.Email).IsRequired();
        builder.Property(client => client.Phone).IsRequired();
        builder.OwnsOne(client => client.CommandNumber, number =>
        {
            number.Property(value => value.AgencyCode).HasColumnName("agency_code");
            number.Property(value => value.Value).HasColumnName("command_value").IsRequired();
        });
        builder.Navigation(client => client.CommandNumber).IsRequired();
        InspectionColumns.Apply(builder);
    }
}

public sealed class RentalConfiguration : IEntityTypeConfiguration<Rental>
{
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        builder.ToTable("rentals");
        builder.Property(rental => rental.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<Agency>().WithMany().HasForeignKey(rental => rental.AgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>().WithMany().HasForeignKey(rental => rental.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(rental => rental.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rental => rental.EquipmentId).IsUnique()
            .HasFilter("status IN ('Draft', 'Active')")
            .HasDatabaseName("ux_rentals_open_equipment");
        builder.HasIndex(rental => rental.ClientId);
        InspectionColumns.Apply(builder);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

public sealed class InspectionConfiguration : IEntityTypeConfiguration<InspectionEntity>
{
    public void Configure(EntityTypeBuilder<InspectionEntity> builder)
    {
        builder.ToTable("inspections");
        builder.Property(inspection => inspection.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(inspection => inspection.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(inspection => inspection.OperatorFirstName).IsRequired();
        builder.HasOne<Rental>().WithMany().HasForeignKey(inspection => inspection.RentalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InspectionTemplate>().WithMany().HasForeignKey(inspection => inspection.TemplateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(inspection => inspection.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(inspection => inspection.Answers).WithOne()
            .HasForeignKey(answer => answer.InspectionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(inspection => inspection.Answers).HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(inspection => new { inspection.RentalId, inspection.Type }).IsUnique();
        InspectionColumns.Apply(builder);
        builder.Property<uint>("xmin").IsRowVersion();
    }
}

public sealed class InspectionAnswerConfiguration : IEntityTypeConfiguration<InspectionAnswer>
{
    public void Configure(EntityTypeBuilder<InspectionAnswer> builder)
    {
        builder.ToTable("inspection_answers");
        builder.Property(answer => answer.Result).HasConversion<string>().HasMaxLength(32);
        builder.Ignore(answer => answer.PhotosIds);
        builder.Property<List<Guid>>("_photos").HasColumnName("photo_ids").HasColumnType("uuid[]");
        builder.HasOne<InspectionTemplatePoint>().WithMany().HasForeignKey(answer => answer.TemplatePointId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(answer => new { answer.InspectionId, answer.TemplatePointId }).IsUnique();
        InspectionColumns.Apply(builder);
        builder.Property<List<Guid>>("_photos").HasColumnName("photo_ids");
    }
}

public sealed class InspectionTemplateConfiguration : IEntityTypeConfiguration<InspectionTemplate>
{
    public void Configure(EntityTypeBuilder<InspectionTemplate> builder)
    {
        builder.ToTable("inspection_templates");
        builder.HasOne<EquipmentCategory>().WithMany().HasForeignKey(template => template.EquipmentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(template => template.Points).WithOne().HasForeignKey(point => point.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(template => template.Points).HasField("_points")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(template => new { template.EquipmentCategoryId, template.CreatedAt });
        InspectionColumns.Apply(builder);
    }
}

public sealed class InspectionTemplatePointConfiguration : IEntityTypeConfiguration<InspectionTemplatePoint>
{
    public void Configure(EntityTypeBuilder<InspectionTemplatePoint> builder)
    {
        builder.ToTable("inspection_template_points");
        builder.Property(point => point.Description).IsRequired();
        InspectionColumns.Apply(builder);
    }
}

public sealed class InspectionMediaConfiguration : IEntityTypeConfiguration<InspectionMedia>
{
    public void Configure(EntityTypeBuilder<InspectionMedia> builder)
    {
        builder.ToTable("inspection_media");
        builder.Property(media => media.ObjectKey).IsRequired();
        builder.Property(media => media.FileName).IsRequired();
        builder.Property(media => media.ContentType).IsRequired();
        builder.HasOne<InspectionEntity>().WithMany().HasForeignKey(media => media.InspectionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<InspectionAnswer>().WithMany().HasForeignKey(media => media.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(media => media.ObjectKey).IsUnique();
        InspectionColumns.Apply(builder);
    }
}
