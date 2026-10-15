namespace MatosKC.Infrastructure.Persistence.Configurations;

using MatosKC.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.EntityType).HasMaxLength(100);
        builder.Property(entry => entry.Operation).HasMaxLength(16);
        builder.Property(entry => entry.ChangesJson).HasColumnType("jsonb");
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId, entry.OccurredAt });
        // No account FK: audit must survive account deletion.
    }
}
