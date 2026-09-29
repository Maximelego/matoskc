namespace MatosKC.Infrastructure.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class InfrastructureTestCollection
    : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "Infrastructure tests";
}
