using MatosKC.Application.Accounts.Ports;
using MatosKC.Infrastructure.Hash;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace MatosKC.Infrastructure.Tests.Hash;

public sealed class IdentityPasswordHasherTests
{
    [Fact]
    public void Hash_AcceptsCorrectPasswordAndRejectsWrongPassword()
    {
        var hasher = new IdentityPasswordHasher(); var hash = hasher.HashPassword("correct-password");
        Assert.Equal(HashVerificationResult.Success, hasher.VerifyPassword("correct-password", hash));
        Assert.Equal(HashVerificationResult.Failed, hasher.VerifyPassword("wrong-password", hash));
    }
    [Fact]
    public void SamePassword_HasIndependentSalts()
    {
        var hasher = new IdentityPasswordHasher(); var a = hasher.HashPassword("password"); var b = hasher.HashPassword("password");
        Assert.NotEqual(a, b);
        Assert.Equal(HashVerificationResult.Success, hasher.VerifyPassword("password", a));
        Assert.Equal(HashVerificationResult.Success, hasher.VerifyPassword("password", b));
    }
    [Fact]
    public void OldParameters_AreAcceptedWithRehashSignal()
    {
        var old = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions { IterationCount = 1000 }));
        var hash = old.HashPassword(new object(), "password");
        Assert.Equal(HashVerificationResult.NeedsRehash, new IdentityPasswordHasher().VerifyPassword("password", hash));
    }
}
