namespace MatosKC.Application.Persistence;

public sealed record AuditEntry(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Operation,
    Guid? AccountId,
    DateTimeOffset OccurredAt,
    string? Reason,
    string ChangesJson
);

public interface IAuditRepository
{
    Task<IReadOnlyList<AuditEntry>> ListAsync(string entityType, Guid entityId,
        int offset, int limit, CancellationToken ct);
}
