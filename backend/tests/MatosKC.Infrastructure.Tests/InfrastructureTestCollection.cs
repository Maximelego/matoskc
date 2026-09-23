namespace MatosKC.Infrastructure.Tests;

[CollectionDefinition(Name)]
public sealed class InfrastructureTestCollection
    : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "Infrastructure tests";
}
