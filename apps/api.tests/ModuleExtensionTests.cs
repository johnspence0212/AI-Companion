using EnterpriseStarter.Companion;
using EnterpriseStarter.ModuleAbstractions;
using EnterpriseStarter.Platform;
using EnterpriseStarter.TestModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Api.Tests;

public sealed class ModuleExtensionTests
{
    [Fact]
    public void Module_ContributesMetadataPermissionsServicesAndContributors()
    {
        var module = new TestEnterpriseModule();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] =
                    "Host=localhost;Database=unused;Username=unused;Password=unused"
            })
            .Build();

        services.AddEnterprisePlatform(configuration, [module]);
        using var provider = services.BuildServiceProvider();

        var catalog = provider.GetRequiredService<PermissionCatalog>();
        Assert.Equal("TestModule", module.Name);
        Assert.Contains(catalog.Definitions, permission =>
            permission.Key == TestEnterpriseModule.Permission
            && permission.Label == "Read test module");
        Assert.Same(module, provider.GetRequiredService<IEnterpriseModule>());
        Assert.NotNull(provider.GetService<TestModuleService>());
        Assert.Equal(
            "test-module",
            provider.GetRequiredService<IEntityModelContributor>().Key);
    }

    [Fact]
    public void Module_ContributesEntityModelWithoutPlatformImportingModuleTypes()
    {
        static DbContextOptions<EnterpriseDbContext> Options() =>
            new DbContextOptionsBuilder<EnterpriseDbContext>()
                .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
                .ReplaceService<IModelCacheKeyFactory, ModuleModelCacheKeyFactory>()
                .Options;

        using var platformOnly = new EnterpriseDbContext(Options(), []);
        Assert.Null(platformOnly.Model.FindEntityType(typeof(TestModuleRecord)));

        var module = new TestEnterpriseModule();
        using var withModule = new EnterpriseDbContext(Options(), module.ModelContributors);
        var entity = withModule.Model.FindEntityType(typeof(TestModuleRecord));

        Assert.NotNull(entity);
        Assert.Equal("TestModuleRecords", entity.GetTableName());
        Assert.Equal(200, entity.FindProperty(nameof(TestModuleRecord.Value))?.GetMaxLength());
    }

    [Fact]
    public void ProductionRegistry_RegistersCompanionModuleAndPermissions()
    {
        var companion = Assert.Single(ModuleRegistry.Production);
        Assert.Equal("Companion", companion.Name);
        Assert.Equal("companion", Assert.Single(companion.ModelContributors).Key);
        Assert.Equal(CompanionPermissions.All, companion.Permissions.Select(permission => permission.Key));
        Assert.Contains(companion.Permissions, permission => permission.Key == CompanionPermissions.ProjectsRead);
        Assert.Contains(companion.Permissions, permission => permission.Key == CompanionPermissions.AiClientsManage);
    }
}
