namespace MatosKC.Infrastructure.Persistence;

using MatosKC.Application.Inspections;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public static class PersistenceExceptionTranslator
{
    public static InspectionOperationException? Translate(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // EF/Npgsql can wrap serialization failures in InvalidOperationException.
        // Classify the complete chain before considering domain validation errors.
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException
                || current is PostgresException { SqlState: "23505" or "40001" or "40P01" })
            {
                return new InspectionOperationException(409,
                    "The resource changed concurrently. Refresh and retry.");
            }
        }

        // Infrastructure wrappers must not be mistaken for business rule violations.
        return exception is InvalidOperationException { InnerException: null }
            ? new InspectionOperationException(422, exception.Message)
            : null;
    }
}
