namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

[Collection(InfrastructureTestCollection.Name)]
public sealed class TemplatePhotoPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task IllustrationMetadata_RoundTripsWithUploaderAndObjectKey()
    {
        Guid id = Guid.NewGuid();
        Guid accountId;
        var now = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        await using (var db = fixture.CreateDbContext())
        {
            var account = new Account("Illustrations", $"photo-{id:N}@example.test", "hash", true, Role.SuperAdmin, null);
            accountId = account.Id;
            db.Accounts.Add(account);
            await new InspectionTemplatePhotoRepository(db).AddAsync(new InspectionTemplatePhoto(id,
                $"inspection-template-photos/{id}", "guide.png", "image/png", 123, account.Id, now),
                TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var read = fixture.CreateDbContext();
        var photo = await new InspectionTemplatePhotoRepository(read).GetByIdAsync(id, TestContext.Current.CancellationToken);
        Assert.NotNull(photo);
        Assert.Equal(accountId, photo.CreatedByAccountId);
        Assert.Equal(now, photo.CreatedAt);
        Assert.Equal(123, photo.ContentLength);
        Assert.Equal($"inspection-template-photos/{id}", photo.ObjectKey);
    }
}
