using MatosKC.Domain.Entities.Agencies;

namespace MatosKC.Domain.Entities.Accounts;

public class Account
{
    public readonly Guid Id;
    public string DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string HashedPassword { get; private set; }
    public readonly Role Role;
    public bool IsActive { get; private set; }
    public readonly Guid? AgencyId;

    public Account(
        Guid id,
        string displayName,
        string? email,
        string hashedPassword,
        bool isActive,
        Role role,
        Guid? agencyId
    )
    {
        // Integrity checks
        CheckParameterIntegrity(id, displayName, email, hashedPassword, role, agencyId);

        Id = id;
        DisplayName = displayName;
        Email = email;
        HashedPassword = hashedPassword;
        Role = role;
        IsActive = isActive;
        AgencyId = agencyId;
    }

    public Account(
        string displayName,
        string? email,
        string hashedPassword,
        bool isActive,
        Role role,
        Guid? agencyId
    ): this(Guid.NewGuid(), displayName, email, hashedPassword, isActive, role, agencyId)
    {
    }

    private static void CheckParameterIntegrity(Guid id, string displayName, string? email, string hashedPassword, Role role, Guid? agencyId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));

        if (agencyId == Guid.Empty)
            throw new ArgumentException("Agency ID cannot be empty.", nameof(agencyId));

        if (!Enum.IsDefined(role))
            throw new ArgumentException("Invalid account role.", nameof(role));

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be null or whitespace.", nameof(displayName));

        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password cannot be null or whitespace.", nameof(hashedPassword));

        if (role != Role.SuperAdmin && agencyId == null)
            throw new ArgumentException("Admin and agency roles require an associated agency.", nameof(agencyId));

        if (role == Role.SuperAdmin && agencyId != null)
            throw new ArgumentException("Super administrator must not be associated with an agency.", nameof(agencyId));

        if (role != Role.Agency && email == null)
            throw new ArgumentException("Non-agency roles require an email.", nameof(email));

        if (role == Role.Agency && email != null)
            throw new ArgumentException("Agency role should not have an email.", nameof(email));

        if (email != null && !IsValidEmail(email))
            throw new ArgumentException("Invalid email format.", nameof(email));
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }


    public void DeactivateAccount()
    {
        IsActive = false;
    }

    public void ActivateAccount()
    {
        IsActive = true;
    }

    public void UpdateDisplayName(string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
            throw new ArgumentException("Display name cannot be null or whitespace.", nameof(newDisplayName));

        DisplayName = newDisplayName;
    }

    public void UpdateEmail(string? newEmail)
    {
        if (Role != Role.Agency && newEmail == null)
            throw new ArgumentException("Non-agency roles require an email.", nameof(newEmail));

        if (Role == Role.Agency && newEmail != null)
            throw new ArgumentException("Agency role should not have an email.", nameof(newEmail));

        if (newEmail != null && !IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email format.", nameof(newEmail));

        Email = newEmail;
    }

    public void UpdateHashedPassword(string newHashedPassword)
    {
        if (string.IsNullOrWhiteSpace(newHashedPassword))
            throw new ArgumentException("Hashed password cannot be null or whitespace.", nameof(newHashedPassword));

        HashedPassword = newHashedPassword;
    }
}
