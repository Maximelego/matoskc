namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class AuditRepository(MatosKCDbContext db) : IAuditRepository
{
    public async Task<IReadOnlyList<AuditEntry>> ListAsync(string entityType, Guid entityId,
        int offset, int limit, CancellationToken ct) =>
        await db.AuditEntries.AsNoTracking()
            .Where(entry => entry.EntityType == entityType && entry.EntityId == entityId)
            .OrderByDescending(entry => entry.OccurredAt).ThenByDescending(entry => entry.Id)
            .Skip(offset).Take(limit).ToListAsync(ct);
}
