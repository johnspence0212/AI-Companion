using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Companion.Infrastructure;
using EnterpriseStarter.ModuleAbstractions;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Companion;

public sealed class CompanionModule : IEnterpriseModule
{
    public string Name => "Companion";

    public IReadOnlyList<ModulePermission> Permissions => CompanionPermissions.Catalog;

    public IReadOnlyList<IEntityModelContributor> ModelContributors { get; } =
        [new CompanionModelContributor()];

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/projects/{id:guid}",
                async (Guid id, EnterpriseDbContext db, CancellationToken cancellationToken) =>
                {
                    var project = await db.Set<Project>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
                    return project is null
                        ? Results.NotFound()
                        : Results.Ok(new { id = project.Id, name = project.Name });
                })
            .RequireAuthorization(CompanionPermissions.ProjectsRead);
    }
}

public static class ModuleRegistry
{
    public static IReadOnlyList<IEnterpriseModule> Production { get; } = [new CompanionModule()];
}
