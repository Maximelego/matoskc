namespace MatosKC.Domain.Entities.Clients;

public sealed class Client : BaseEntity
{
    private Client()
    {
        DisplayName = null!;
        Email = null!;
        Phone = null!;
        CommandNumber = null!;
    }

    public string DisplayName { get; private set; }

    public string Email { get; private set; }

    public string Phone { get; private set; }

    public CommandNumber CommandNumber { get; private set; }

    public Client(
        Guid id,
        string displayName,
        string email,
        string phone,
        CommandNumber commandNumber
    ): base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentNullException.ThrowIfNull(commandNumber);

        Id = id;
        DisplayName = displayName.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
        CommandNumber = commandNumber;
    }
}
