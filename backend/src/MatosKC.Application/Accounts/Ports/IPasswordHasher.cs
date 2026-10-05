namespace MatosKC.Application.Accounts.Ports;

public interface IPasswordHasher
{
    public string HashPassword(string password);

    public bool VerifyPassword(string password, string hashedPassword);

    public bool NeedsRehash(string hashedPassword);
}
