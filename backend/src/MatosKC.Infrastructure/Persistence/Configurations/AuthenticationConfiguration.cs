using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.AuthenticationSession;
namespace MatosKC.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable(
            "accounts", t => t.HasCheckConstraint("ck_accounts_role_agency_email",
                            "(role = 'SuperAdmin' AND agency_id IS NULL AND email IS NOT NULL) " +
                            "OR (role = 'Admin' AND agency_id IS NOT NULL AND email IS NOT NULL) " +
                            "OR (role = 'Agency' AND agency_id IS NOT NULL AND email IS NULL)"));

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.DisplayName).HasColumnName("display_name").IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("citext");
        b.Property(x => x.HashedPassword).HasColumnName("hashed_password").IsRequired();
        b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.AgencyId).HasColumnName("agency_id");

        b.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_accounts_email");

        b.HasIndex(x => x.AgencyId)
            .IsUnique()
            .HasFilter("role = 'Agency'")
            .HasDatabaseName("ux_accounts_shared_agency");

        b.HasOne<Agency>()
            .WithMany()
            .HasForeignKey(x => x.AgencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> b)
    {
        b.ToTable("agencies");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("name").IsRequired();
        b.Property(x => x.Code).HasColumnName("code");

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_agencies_code");
    }
}

public sealed class AuthenticationSessionConfiguration
    : IEntityTypeConfiguration<AuthenticationSession>
{
    public void Configure(EntityTypeBuilder<AuthenticationSession> b)
    {
        b.ToTable("authentication_sessions");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.AccountId).HasColumnName("account_id");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");

        b.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.AccountId);

        b.HasIndex(x => x.ExpiresAt);
    }
}
