namespace MatosKC.Infrastructure.Hash;

using MatosKC.Application.Accounts.Ports;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher;

    public IdentityPasswordHasher()
    {
        _hasher = new PasswordHasher<object>();
    }

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(new object(), password);
    }

    public HashVerificationResult VerifyPassword(string password, string hashedPassword)
    {
        return _hasher.VerifyHashedPassword(new object(), hashedPassword, password) switch
        {
            PasswordVerificationResult.Success => HashVerificationResult.Success,
            PasswordVerificationResult.Failed => HashVerificationResult.Failed,
            PasswordVerificationResult.SuccessRehashNeeded => HashVerificationResult.NeedsRehash,
            _ => throw new InvalidOperationException("Unexpected password verification result.")
        };
    }
}
