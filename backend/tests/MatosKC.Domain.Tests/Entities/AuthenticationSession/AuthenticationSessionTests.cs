using Session = MatosKC.Domain.Entities.AuthenticationSession.AuthenticationSession;

namespace MatosKC.Domain.Tests.Entities.AuthenticationSession;

public class AuthenticationSessionTests
{
    [Fact]
    public void Constructor_CreatesValidSessionWithRequestedDuration()
    {
        Guid accountId = Guid.NewGuid();
        DateTime before = DateTime.UtcNow;
        var session = new Session(accountId, TimeSpan.FromHours(1));
        DateTime after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(accountId, session.AccountId);
        Assert.InRange(session.CreatedAt, before, after);
        Assert.Equal(DateTimeKind.Utc, session.CreatedAt.Kind);
        Assert.Equal(session.CreatedAt.AddHours(1), session.ExpiresAt);
        Assert.Null(session.RevokedAt);
        Assert.True(session.IsValid());
    }

    [Fact]
    public void Constructor_GeneratesDistinctIds()
    {
        Guid accountId = Guid.NewGuid();
        var first = new Session(accountId, TimeSpan.FromHours(1));
        var second = new Session(accountId, TimeSpan.FromHours(1));
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Constructor_RejectsEmptyAccountId() =>
        Assert.Throws<ArgumentException>(() => new Session(Guid.Empty, TimeSpan.FromHours(1)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveDuration(int seconds) =>
        Assert.Throws<ArgumentException>(() => new Session(Guid.NewGuid(), TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void Revoke_InvalidatesSessionAndRecordsUtcTime()
    {
        var session = new Session(Guid.NewGuid(), TimeSpan.FromHours(1));
        DateTime before = DateTime.UtcNow;
        session.Revoke();
        DateTime after = DateTime.UtcNow;
        Assert.InRange(session.RevokedAt!.Value, before, after);
        Assert.Equal(DateTimeKind.Utc, session.RevokedAt!.Value.Kind);
        Assert.False(session.IsValid());
    }

    [Fact]
    public void IsValid_RejectsExpirationBoundaryAndFutureCreation()
    {
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        var session = new Session(Guid.NewGuid(), TimeSpan.FromHours(1), now);
        Assert.False(session.IsValid(now.AddTicks(-1)));
        Assert.True(session.IsValid(now));
        Assert.True(session.IsValid(session.ExpiresAt.AddTicks(-1)));
        Assert.False(session.IsValid(session.ExpiresAt));
        Assert.False(session.IsValid(session.ExpiresAt.AddTicks(1)));
    }

    [Fact]
    public void Revoke_PreservesFirstRevocation()
    {
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        var session = new Session(Guid.NewGuid(), TimeSpan.FromHours(1), now);
        session.Revoke(now.AddMinutes(1));
        session.Revoke(now.AddMinutes(2));
        Assert.Equal(now.AddMinutes(1), session.RevokedAt);
        Assert.False(session.IsValid(now.AddMinutes(3)));
    }

    [Fact]
    public void Revoke_RejectsTimeBeforeCreation()
    {
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        var session = new Session(Guid.NewGuid(), TimeSpan.FromHours(1), now);
        Assert.Throws<ArgumentException>(() => session.Revoke(now.AddTicks(-1)));
        Assert.Null(session.RevokedAt);
    }
}
