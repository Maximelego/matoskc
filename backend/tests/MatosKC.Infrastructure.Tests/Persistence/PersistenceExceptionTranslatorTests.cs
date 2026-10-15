namespace MatosKC.Infrastructure.Tests.Persistence;

using MatosKC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed class PersistenceExceptionTranslatorTests
{
    [Theory]
    [InlineData("23505")]
    [InlineData("40001")]
    [InlineData("40P01")]
    public void PostgreSqlConflict_Is409AtEveryWrapperDepth(string sqlState)
    {
        Exception error = new PostgresException("Conflict", "ERROR", "ERROR", sqlState);

        for (int depth = 0; depth < 4; depth++)
        {
            Assert.Equal(409, PersistenceExceptionTranslator.Translate(error)!.StatusCode);
            error = depth == 0
                ? new DbUpdateException("Save failed.", error)
                : new InvalidOperationException("A transient failure was detected.", error);
        }
    }

    [Fact]
    public void WrappedOptimisticConcurrency_Is409()
    {
        var error = new InvalidOperationException("Wrapped.", new DbUpdateConcurrencyException());

        Assert.Equal(409, PersistenceExceptionTranslator.Translate(error)!.StatusCode);
    }

    [Fact]
    public void DomainRuleViolation_Remains422()
    {
        var result = PersistenceExceptionTranslator.Translate(new InvalidOperationException("Inspection is already validated."));

        Assert.Equal(422, result!.StatusCode);
        Assert.Equal("Inspection is already validated.", result.Message);
    }

    [Fact]
    public void OtherDatabaseErrors_AreNotMisreportedAsBusinessValidation()
    {
        var sql = new PostgresException("Connection failure", "ERROR", "ERROR", "08006");

        Assert.Null(PersistenceExceptionTranslator.Translate(new InvalidOperationException("Transient failure.", sql)));
        Assert.Null(PersistenceExceptionTranslator.Translate(new DbUpdateException("Save failed.", sql)));
    }
}
