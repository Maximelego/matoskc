namespace MatosKC.Api.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiTestCollection : ICollectionFixture<MatosKCApiFactory>
{
    public const string Name = "API tests";
}
