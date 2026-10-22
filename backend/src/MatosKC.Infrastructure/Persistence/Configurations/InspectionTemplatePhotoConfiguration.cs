namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InspectionTemplatePhotoConfiguration : IEntityTypeConfiguration<InspectionTemplatePhoto>
{
    public void Configure(EntityTypeBuilder<InspectionTemplatePhoto> builder)
    {
        builder.ToTable("inspection_template_photos");
        builder.Property(value => value.AgencyId).HasColumnName("agency_id");
        builder.HasOne<MatosKC.Domain.Entities.Agencies.Agency>().WithMany().HasForeignKey(value => value.AgencyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(photo => photo.ObjectKey).IsUnique();
        builder.HasOne<Account>().WithMany().HasForeignKey(photo => photo.CreatedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        InspectionColumns.Apply(builder);
    }
}
