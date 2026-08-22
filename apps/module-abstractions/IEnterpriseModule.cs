using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.ModuleAbstractions;

public sealed record ModulePermission(string Key, string Label, string Group);

public interface IEntityModelContributor
{
    string Key { get; }
    void Configure(ModelBuilder modelBuilder);
}

public interface IEnterpriseModule
{
    string Name { get; }
    IReadOnlyList<ModulePermission> Permissions { get; }
    IReadOnlyList<IEntityModelContributor> ModelContributors { get; }
    void AddServices(IServiceCollection services, IConfiguration configuration);
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public static class ModuleRegistry
{
    public static IReadOnlyList<IEnterpriseModule> Production { get; } = [];
}
