using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;

namespace MatosKC.Domain.Tests.Entities.Accounts;

public class AccountTests
{
    private const string ValidEmail = "maxime@example.com";
    private const string ValidHash = "test-password-hash";

    private static Agency CreateAgency() =>
        new(Guid.NewGuid(), "Épinal", 83);

    private static Account CreateAdmin() =>
        new(
            "Maxime",
            ValidEmail,
            ValidHash,
            true,
            Role.Admin,
            CreateAgency().Id
        );

    [Theory]
    [InlineData(Role.SuperAdmin, true)]
    [InlineData(Role.SuperAdmin, false)]
    [InlineData(Role.Admin, true)]
    [InlineData(Role.Admin, false)]
    [InlineData(Role.Agency, true)]
    [InlineData(Role.Agency, false)]
    public void Constructor_WithValidParameters_PreservesValues(
        Role role,
        bool isActive)
    {
        Guid id = Guid.NewGuid();
        string? email = role == Role.Agency ? null : ValidEmail;
        Guid? agencyId = role == Role.SuperAdmin ? null : CreateAgency().Id;

        var account = new Account(
            id, "Compte test", email, ValidHash, isActive, role, agencyId);

        Assert.Equal(id, account.Id);
        Assert.Equal("Compte test", account.DisplayName);
        Assert.Equal(email, account.Email);
        Assert.Equal(ValidHash, account.HashedPassword);
        Assert.Equal(role, account.Role);
        Assert.Equal(isActive, account.IsActive);
        Assert.Equal(agencyId, account.AgencyId);
    }

    [Fact]
    public void Constructor_WithoutId_GeneratesDistinctNonEmptyIds()
    {
        Account first = CreateAdmin();
        Account second = CreateAdmin();

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                Guid.Empty, "Maxime", ValidEmail,
                ValidHash, true, Role.Admin, CreateAgency().Id));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Constructor_WithUnknownRole_Throws(int role)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                "Maxime", ValidEmail, ValidHash,
                true, (Role)role, CreateAgency().Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDisplayName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                name!, ValidEmail, ValidHash,
                true, Role.Admin, CreateAgency().Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidHash_Throws(string? hash)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                "Maxime", ValidEmail, hash!,
                true, Role.Admin, CreateAgency().Id));
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public void Constructor_WithEmptyAgencyId_Throws(Role role)
    {
        string? email = role == Role.Agency ? null : ValidEmail;

        Assert.Throws<ArgumentException>(() =>
            new Account("Compte test", email, ValidHash, true, role, Guid.Empty));
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Agency)]
    public void Constructor_WithMissingAgency_Throws(Role role)
    {
        string? email = role == Role.Agency ? null : ValidEmail;

        Assert.Throws<ArgumentException>(() =>
            new Account("Compte test", email, ValidHash, true, role, null));
    }

    [Fact]
    public void Constructor_WithSuperAdminAssociatedWithAgency_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                "Maxime", ValidEmail, ValidHash,
                true, Role.SuperAdmin, CreateAgency().Id));
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.SuperAdmin)]
    public void Constructor_WithMissingEmailForIndividualAccount_Throws(
        Role role)
    {
        Guid? agencyId = role == Role.Admin ? CreateAgency().Id : null;

        Assert.Throws<ArgumentException>(() =>
            new Account("Maxime", null, ValidHash, true, role, agencyId));
    }

    [Fact]
    public void Constructor_WithEmailForAgencyAccount_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                "Agence Épinal", ValidEmail, ValidHash,
                true, Role.Agency, CreateAgency().Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("maxime@")]
    public void Constructor_WithInvalidEmail_Throws(string email)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(
                "Maxime", email, ValidHash,
                true, Role.Admin, CreateAgency().Id));
    }

    [Fact]
    public void DeactivateAccount_DisablesAccount()
    {
        Account account = CreateAdmin();

        account.DeactivateAccount();

        Assert.False(account.IsActive);
    }

    [Fact]
    public void ActivateAccount_EnablesAccount()
    {
        Account account = CreateAdmin();
        account.DeactivateAccount();

        account.ActivateAccount();

        Assert.True(account.IsActive);
    }

    [Fact]
    public void UpdateDisplayName_WithValidName_UpdatesValue()
    {
        Account account = CreateAdmin();

        account.UpdateDisplayName("Nouveau nom");

        Assert.Equal("Nouveau nom", account.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDisplayName_WithInvalidName_PreservesPreviousValue(
        string? name)
    {
        Account account = CreateAdmin();
        string previous = account.DisplayName;

        Assert.Throws<ArgumentException>(() =>
            account.UpdateDisplayName(name!));

        Assert.Equal(previous, account.DisplayName);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.SuperAdmin)]
    public void UpdateEmail_WithValidEmail_UpdatesValue(Role role)
    {
        Guid? agencyId = role == Role.Admin ? CreateAgency().Id : null;
        var account = new Account(
            "Maxime", ValidEmail, ValidHash, true, role, agencyId);

        account.UpdateEmail("nouveau@example.com");

        Assert.Equal("nouveau@example.com", account.Email);
    }

    [Theory]
    [InlineData(Role.Admin, null)]
    [InlineData(Role.Admin, "")]
    [InlineData(Role.Admin, "   ")]
    [InlineData(Role.Admin, "invalid-email")]
    [InlineData(Role.SuperAdmin, null)]
    [InlineData(Role.SuperAdmin, "")]
    [InlineData(Role.SuperAdmin, "   ")]
    [InlineData(Role.SuperAdmin, "invalid-email")]
    public void UpdateEmail_WithInvalidEmail_PreservesPreviousValue(
        Role role,
        string? email)
    {
        Guid? agencyId = role == Role.Admin ? CreateAgency().Id : null;
        var account = new Account(
            "Maxime", ValidEmail, ValidHash, true, role, agencyId);

        Assert.Throws<ArgumentException>(() =>
            account.UpdateEmail(email));

        Assert.Equal(ValidEmail, account.Email);
    }

    [Fact]
    public void UpdateEmail_WithEmailForAgencyAccount_Throws()
    {
        var account = new Account(
            "Agence Épinal", null, ValidHash,
            true, Role.Agency, CreateAgency().Id);

        Assert.Throws<ArgumentException>(() =>
            account.UpdateEmail(ValidEmail));

        Assert.Null(account.Email);
    }

    [Fact]
    public void UpdateEmail_WithNullForAgencyAccount_RemainsValid()
    {
        var account = new Account(
            "Agence Épinal", null, ValidHash,
            true, Role.Agency, CreateAgency().Id);

        account.UpdateEmail(null);

        Assert.Null(account.Email);
    }

    [Fact]
    public void UpdateHashedPassword_WithValidHash_UpdatesValue()
    {
        Account account = CreateAdmin();

        account.UpdateHashedPassword("new-test-hash");

        Assert.Equal("new-test-hash", account.HashedPassword);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateHashedPassword_WithInvalidHash_PreservesPreviousValue(
        string? hash)
    {
        Account account = CreateAdmin();

        Assert.Throws<ArgumentException>(() =>
            account.UpdateHashedPassword(hash!));

        Assert.Equal(ValidHash, account.HashedPassword);
    }
}


