namespace MatosKC.Infrastructure.Persistence;

using System.Data;
using Microsoft.EntityFrameworkCore;
using MatosKC.Application.Persistence;
using MatosKC.Application.Inspections;

public sealed class EfUnitOfWork(MatosKCDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            T result = await operation(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();

            var translated = PersistenceExceptionTranslator.Translate(exception);

            if (translated is not null)
            {
                throw translated;
            }

            throw;
        }
    }
}
