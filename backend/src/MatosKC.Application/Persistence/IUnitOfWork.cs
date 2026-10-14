namespace MatosKC.Application.Persistence;

/// <summary>Runs an operation and commits all participating repositories atomically.</summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default
    );
}
