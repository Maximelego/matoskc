namespace MatosKC.Domain.Entities.AuthenticationSession;

public class AuthenticationSession
{
    private AuthenticationSession() { }

    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public AuthenticationSession(Guid accountId, TimeSpan sessionDuration)
        : this(accountId, sessionDuration, DateTime.UtcNow) { }

    public AuthenticationSession(Guid accountId, TimeSpan sessionDuration, DateTime now)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty.", nameof(accountId));
        if (sessionDuration <= TimeSpan.Zero)
            throw new ArgumentException("Session duration must be greater than zero.", nameof(sessionDuration));
        EnsureUtc(now);
        Id = Guid.NewGuid();
        AccountId = accountId;
        CreatedAt = now;
        ExpiresAt = now.Add(sessionDuration);
    }

    public bool IsValid() => IsValid(DateTime.UtcNow);

    public bool IsValid(DateTime now)
    {
        EnsureUtc(now);
        return now >= CreatedAt && now < ExpiresAt && RevokedAt == null;
    }

    public void Revoke() => Revoke(DateTime.UtcNow);

    public void Revoke(DateTime now)
    {
        EnsureUtc(now);
        if (now < CreatedAt)
            throw new ArgumentException("Revocation cannot precede creation.", nameof(now));
        RevokedAt ??= now;
    }

    private static void EnsureUtc(DateTime now)
    {
        if (now.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must use UTC.", nameof(now));
    }
}
