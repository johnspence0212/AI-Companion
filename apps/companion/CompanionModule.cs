using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Companion.Infrastructure;
using EnterpriseStarter.Companion.Mcp;
using EnterpriseStarter.ModuleAbstractions;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;

namespace EnterpriseStarter.Companion;

public sealed class CompanionModule : IEnterpriseModule
{
    public string Name => "Companion";

    public IReadOnlyList<ModulePermission> Permissions => CompanionPermissions.Catalog;

    public IReadOnlyList<IEntityModelContributor> ModelContributors { get; } =
        [new CompanionModelContributor()];

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AiClientService>();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, AiClientAuthenticationHandler>(AiClientAuth.Scheme, _ => { });
        services.AddAuthorization(options =>
            options.AddPolicy(AiClientAuth.Policy, policy =>
            {
                policy.AddAuthenticationSchemes(AiClientAuth.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(AiClientAuth.ClientIdClaim);
            }));
        services.AddMcpServer()
            .WithHttpTransport(options =>
                options.SessionMode = HttpServerSessionMode.StatefulForInitializeClients)
            .WithTools<CompanionPingTools>();
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

        var clients = endpoints.MapGroup("/api/v1/ai-clients")
            .RequireAuthorization(CompanionPermissions.AiClientsManage);
        clients.MapGet("/", async (AiClientService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(cancellationToken)));
        clients.MapPost(
            "/",
            async (CreateAiClientRequest request, ClaimsPrincipal user, AiClientService service, CancellationToken cancellationToken) =>
            {
                var ownerId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["name"] = ["Name is required."]
                    });
                }

                var created = await service.CreateAsync(ownerId, request.Name, cancellationToken);
                return Results.Created($"/api/v1/ai-clients/{created.Id}", created);
            });
        clients.MapPost(
            "/{id:guid}/revoke",
            async (Guid id, AiClientService service, CancellationToken cancellationToken) =>
                await service.RevokeAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

        endpoints.MapMcp("/mcp").RequireAuthorization(AiClientAuth.Policy);
    }
}

public sealed record CreateAiClientRequest([Required, StringLength(200)] string Name);

public static class ModuleRegistry
{
    public static IReadOnlyList<IEnterpriseModule> Production { get; } = [new CompanionModule()];
}
