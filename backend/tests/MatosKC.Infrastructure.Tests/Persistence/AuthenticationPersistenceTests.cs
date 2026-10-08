using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Session = MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession;
namespace MatosKC.Infrastructure.Tests.Persistence;

[Collection(InfrastructureTestCollection.Name)]
public sealed class AuthenticationPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Session_RoundTripPreservesIdentityUtcDatesAndRevocation()
    {
        var account = new Account("Test", $"session-{Guid.NewGuid():N}@example.com", "hash", true, Role.SuperAdmin, null);
        var session = new Session(account.Id, TimeSpan.FromHours(1));
        await using (var db = fixture.CreateDbContext())
        {
            db.Accounts.Add(account); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            await new AuthenticationSessionRepository(db).AddAsync(session, TestContext.Current.CancellationToken);
        }
        await using (var db = fixture.CreateDbContext())
        {
            var stored = await new AuthenticationSessionRepository(db).GetByIdAsync(session.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(stored); Assert.Equal(account.Id, stored.AccountId);
            // PostgreSQL timestamp precision is one microsecond.
            Assert.InRange(Math.Abs((stored.CreatedAt - session.CreatedAt).Ticks), 0, 9);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind); Assert.True(stored.IsValid());
            stored.Revoke(); await new AuthenticationSessionRepository(db).UpdateAsync(stored, TestContext.Current.CancellationToken);
        }
        await using (var db = fixture.CreateDbContext())
        {
            var stored = await new AuthenticationSessionRepository(db).GetByIdAsync(session.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(stored); Assert.NotNull(stored.RevokedAt); Assert.False(stored.IsValid());
        }
    }
    [Fact]
    public async Task SharedAccount_UniquenessIncludesDisabledAccounts()
    {
        var agency = new Agency("Test", Random.Shared.Next(100000, int.MaxValue));
        await using (var db = fixture.CreateDbContext())
        {
            db.AddRange(agency, new Account("First", null, "hash", false, Role.Agency, agency.Id));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using (var db = fixture.CreateDbContext())
        {
            db.Accounts.Add(new Account("Second", null, "hash", true, Role.Agency, agency.Id));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        }
    }
    [Fact]
    public async Task SharedAccountLookup_DoesNotReturnAgencyAdministrator()
    {
        var agency = new Agency("Test", Random.Shared.Next(100000, int.MaxValue));
        await using var db = fixture.CreateDbContext();
        db.AddRange(agency, new Account("Admin", $"admin-{Guid.NewGuid():N}@example.com", "hash", true, Role.Admin, agency.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new AccountRepository(db);
        Assert.Null(await repository.GetByAgencyIdAsync(agency.Id, TestContext.Current.CancellationToken));
        var shared = new Account("Agency", null, "hash", true, Role.Agency, agency.Id);
        await repository.AddAsync(shared, TestContext.Current.CancellationToken);
        Assert.Equal(shared.Id, (await repository.GetByAgencyIdAsync(agency.Id, TestContext.Current.CancellationToken))!.Id);
    }
    [Fact]
    public async Task DeleteAccount_RemovesStoredSessions()
    {
        var account = new Account("Test", $"delete-{Guid.NewGuid():N}@example.com", "hash", true, Role.SuperAdmin, null);
        var session = new Session(account.Id, TimeSpan.FromHours(1));
        await using var db = fixture.CreateDbContext(); db.AddRange(account, session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await new AccountRepository(db).DeleteAsync(account, TestContext.Current.CancellationToken);
        Assert.False(await db.AuthenticationSessions.AnyAsync(x => x.Id == session.Id, TestContext.Current.CancellationToken));
    }
}
