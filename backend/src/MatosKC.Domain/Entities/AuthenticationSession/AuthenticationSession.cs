namespace MatosKC.Domain.Entities.AuthenticationSession;

public class AuthenticationSession
{

    public Guid Id { get; }
    public Guid AccountId { get; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }
    public DateTime RevokedAt { get; set;}

    public AuthenticationSession(Guid accountId, TimeSpan sessionDuration)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID cannot be empty.", nameof(accountId));
        if (sessionDuration <= TimeSpan.Zero)
            throw new ArgumentException("Session duration must be greater than zero.", nameof(sessionDuration));

        Id = Guid.NewGuid();
        AccountId = accountId;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.Add(sessionDuration);
        RevokedAt = DateTime.MinValue;
    }

    public bool IsValid() {
        return CreatedAt != DateTime.MinValue && ExpiresAt > DateTime.UtcNow && RevokedAt == DateTime.MinValue;
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
