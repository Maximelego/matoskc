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

        if (value.Length != 6 || value.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException("Command number must contain exactly 6 ASCII digits.", nameof(value));
        }

        AgencyCode = agencyCode;
        Value = value;
    }
}
