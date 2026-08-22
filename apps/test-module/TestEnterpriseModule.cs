using EnterpriseStarter.ModuleAbstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.TestModule;

public sealed class TestModuleRecord
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public sealed class TestModuleService;

public sealed class TestModuleModelContributor : IEntityModelContributor
{
    public string Key => "test-module";

    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestModuleRecord>(entity =>
        {
            entity.ToTable("TestModuleRecords");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Value).HasMaxLength(200).IsRequired();
        });
    }
}

public sealed class TestEnterpriseModule : IEnterpriseModule
{
    public const string Permission = "test-module.read";

    public string Name => "TestModule";

    public IReadOnlyList<ModulePermission> Permissions { get; } =
        [new(Permission, "Read test module", "Test module")];

    public IReadOnlyList<IEntityModelContributor> ModelContributors { get; } =
        [new TestModuleModelContributor()];

    public void AddServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton<TestModuleService>();

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/api/v1/test-module",
                (TestModuleService _) => Results.Ok(new { module = Name }))
            .RequireAuthorization(Permission);
}
