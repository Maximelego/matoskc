using MatosKC.Application.Accounts.List;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Application.Administration;
using MatosKC.Application.Agencies.List;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Auth.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.AuthenticationSession;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace MatosKC.Infrastructure.Persistence.Repositories;
internal static class AuthenticationPersistence
{
    public static async Task SaveAsync(MatosKCDbContext db, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" })
        { throw new AdministrationException(409, "Email, agency code or shared agency account already exists."); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23503" })
        { throw new AdministrationException(409, "The resource is referenced or no longer exists."); }
    }
}
public sealed class AccountRepository(MatosKCDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => db.Accounts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Account?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => db.Accounts.SingleOrDefaultAsync(x => x.Email == email.Trim(), cancellationToken);
    public Task<Account?> GetByAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken = default) => db.Accounts.SingleOrDefaultAsync(x => x.AgencyId == agencyId && x.Role == Role.Agency, cancellationToken);
    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => db.Accounts.AnyAsync(x => x.Id == id, cancellationToken);
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) => db.Accounts.AnyAsync(x => x.Email == email.Trim(), cancellationToken);
    public Task<bool> ExistsAgencyAccountAsync(Guid agencyId, CancellationToken cancellationToken = default) => db.Accounts.AnyAsync(x => x.AgencyId == agencyId && x.Role == Role.Agency, cancellationToken);
    public async Task<IReadOnlyList<Account>> ListByQueryAsync(ListAccountsQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.Accounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.DisplayName)) q = q.Where(x => x.DisplayName.Contains(query.DisplayName));
        if (!string.IsNullOrWhiteSpace(query.Email)) q = q.Where(x => x.Email == query.Email);
        if (query.Role.HasValue) q = q.Where(x => x.Role == query.Role.Value);
        return await q.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);
    }
    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    { db.Accounts.Add(account); await AuthenticationPersistence.SaveAsync(db, cancellationToken); return account; }
    public async Task<Account> UpdateAsync(Account account, CancellationToken cancellationToken = default)
    { db.Accounts.Update(account); await AuthenticationPersistence.SaveAsync(db, cancellationToken); return account; }
    public async Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
    { db.Accounts.Remove(account); await AuthenticationPersistence.SaveAsync(db, cancellationToken); }
}
public sealed class AgencyRepository(MatosKCDbContext db) : IAgencyRepository, IAdministrationRepository
{
    public Task<Agency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => db.Agencies.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Agency?> GetByCodeAsync(int code, CancellationToken cancellationToken = default) => db.Agencies.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) => db.Agencies.AnyAsync(x => x.Id == id, cancellationToken);
    public async Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.Agencies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Name)) q = q.Where(x => x.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            if (!int.TryParse(query.Code, out int code)) throw new ArgumentException("Invalid agency code.");
            q = q.Where(x => x.Code == code);
        }
        return await q.OrderBy(x => x.Code).ToArrayAsync(cancellationToken);
    }
    public async Task<Agency> AddAsync(Agency agency, CancellationToken cancellationToken = default)
    { db.Agencies.Add(agency); await AuthenticationPersistence.SaveAsync(db, cancellationToken); return agency; }
    public async Task<Agency> UpdateAgencyAsync(Agency agency, CancellationToken ct)
    { db.Agencies.Update(agency); await AuthenticationPersistence.SaveAsync(db, ct); return agency; }
    public async Task DeleteAgencyAsync(Agency agency, CancellationToken ct)
    { db.Agencies.Remove(agency); await AuthenticationPersistence.SaveAsync(db, ct); }
    public Task<bool> AgencyCodeExistsAsync(int code, Guid? exceptId, CancellationToken ct) => db.Agencies.AnyAsync(x => x.Code == code && (!exceptId.HasValue || x.Id != exceptId.Value), ct);
    public Task<bool> AgencyHasAccountsAsync(Guid agencyId, CancellationToken ct) => db.Accounts.AnyAsync(x => x.AgencyId == agencyId, ct);
    public async Task RevokeAccountSessionsAsync(Guid accountId, DateTime now, CancellationToken ct)
    {
        var sessions = await db.AuthenticationSessions.Where(x => x.AccountId == accountId && x.RevokedAt == null).ToArrayAsync(ct);
        foreach (var session in sessions) session.Revoke(now);
    }
}
public sealed class AuthenticationSessionRepository(MatosKCDbContext db) : IAuthenticationSessionRepository
{
    public Task<AuthenticationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => db.AuthenticationSessions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task AddAsync(AuthenticationSession session, CancellationToken cancellationToken = default)
    { db.AuthenticationSessions.Add(session); await AuthenticationPersistence.SaveAsync(db, cancellationToken); }
    public async Task UpdateAsync(AuthenticationSession session, CancellationToken cancellationToken = default)
    { db.AuthenticationSessions.Update(session); await AuthenticationPersistence.SaveAsync(db, cancellationToken); }
}
