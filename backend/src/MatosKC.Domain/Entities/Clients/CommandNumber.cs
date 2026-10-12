namespace MatosKC.Domain.Entities.Clients;

public sealed record CommandNumber
{
    public int AgencyCode { get; }
    public string Value { get; }

    public CommandNumber(int agencyCode, string value)
    {
        if (agencyCode <= 0)
        {
            throw new ArgumentException("Agency code must be a positive integer.", nameof(agencyCode));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Command number cannot be null or empty.", nameof(value));
        }

        if (value.Length < 6)
        {
            throw new ArgumentException("Command number must be 6 characters long.", nameof(value));
        }

        AgencyCode = agencyCode;
        Value = value;
    }
}
