namespace EnterpriseStarter.Api.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiIntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "API integration";
}
