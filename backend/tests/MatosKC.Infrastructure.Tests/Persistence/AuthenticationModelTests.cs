using MatosKC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Tests.Persistence;
public sealed class AuthenticationModelTests
{
    [Fact]
    public void AuthenticationEntities_HaveKeysAndRelationships()
    {
        using var db = new MatosKCDbContext(new DbContextOptionsBuilder<MatosKCDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        var session = db.Model.FindEntityType(typeof(MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession));
        Assert.NotNull(session); Assert.NotNull(session.FindPrimaryKey()); Assert.Single(session.GetForeignKeys());
        var account = db.Model.FindEntityType(typeof(MatosKC.Domain.Entities.Accounts.Account));
        Assert.NotNull(account); Assert.Contains(account.GetIndexes(), x => x.IsUnique && x.GetDatabaseName() == "ux_accounts_shared_agency");
    }
}
