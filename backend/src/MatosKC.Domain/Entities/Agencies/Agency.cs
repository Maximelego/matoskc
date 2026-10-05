namespace MatosKC.Domain.Entities.Agencies;

public class Agency
{
    public Guid Id { get; private set; }
    public int Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public Agency(Guid id, string name, int code, bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

        if (code <= 0)
            throw new ArgumentException("Code must be a positive integer.", nameof(code));

        Id = id;
        Name = name;
        Code = code;
        IsActive = isActive;
    }

    public Agency(string name, int code, bool isActive)
        : this(Guid.NewGuid(), name, code, isActive)
    {
    }
}
