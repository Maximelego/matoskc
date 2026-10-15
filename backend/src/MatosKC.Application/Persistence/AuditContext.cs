namespace MatosKC.Application.Persistence;

// Scoped to one request. Background operations have no account identifier.
public sealed class AuditContext
{
    public Guid? AccountId { get; set; }
    public string? Reason { get; set; }
}
