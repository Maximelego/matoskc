namespace MatosKC.Infrastructure.Persistence;

using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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

            if (exception is DbUpdateConcurrencyException
                || exception is PostgresException { SqlState: "40001" or "40P01" }
                || exception is DbUpdateException { InnerException: PostgresException { SqlState: "23505" or "40001" or "40P01" } })
            {
                throw new InspectionOperationException(409, "The resource changed concurrently. Refresh and retry.");
            }

            if (exception is InvalidOperationException)
            {
                throw new InspectionOperationException(422, exception.Message);
            }

            throw;
        }
    }
}
