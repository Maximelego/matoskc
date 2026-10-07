namespace MatosKC.Application.Accounts.Ports;


public interface IPasswordHasher
{
    public string HashPassword(string password);

    public HashVerificationResult VerifyPassword(string password, string hashedPassword);
}
