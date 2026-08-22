using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Api;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Infrastructure;
using EnterpriseStarter.Companion.Mcp;
using EnterpriseStarter.ModuleAbstractions;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        services.AddScoped<ProjectService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<IssueService>();
        services.AddScoped<IAfterSignInHandler>(provider => provider.GetRequiredService<ProjectService>());
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
            .WithTools<CompanionPingTools>()
            .WithTools<ProjectTools>()
            .WithTools<DocumentTools>()
            .WithTools<IssueTools>()
            .WithResources<ProjectContextResources>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/v1/projects");
        projects.MapGet("/", async (ProjectService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAsync(cancellationToken)))
            .RequireAuthorization(CompanionPermissions.ProjectsRead);
        projects.MapPost(
                "/",
                async (CreateProjectRequest request, ClaimsPrincipal user, ProjectService service, CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Name))
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["name"] = ["Name is required."]
                        });
                    }

                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    var created = await service.CreateAsync(userId, userId, clientId, request.Name, cancellationToken);
                    return Results.Created($"/api/v1/projects/{created.Id}", created);
                })
            .RequireAuthorization(CompanionPermissions.ProjectsManage);
        projects.MapGet(
                "/{idOrSlug}",
                async (string idOrSlug, ProjectService service, CancellationToken cancellationToken) =>
                {
                    var project = await service.GetAsync(idOrSlug, cancellationToken);
                    return project is null ? Results.NotFound() : Results.Ok(project);
                })
            .RequireAuthorization(CompanionPermissions.ProjectsRead);
        projects.MapPost(
                "/{id:guid}/archive",
                async (Guid id, ClaimsPrincipal user, ProjectService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    var archived = await service.ArchiveAsync(id, userId, clientId, cancellationToken);
                    return archived is null ? Results.NotFound() : Results.NoContent();
                })
            .RequireAuthorization(CompanionPermissions.ProjectsManage);
        projects.MapGet(
                "/{idOrSlug}/context",
                async (string idOrSlug, ProjectService service, CancellationToken cancellationToken) =>
                {
                    var context = await service.GetContextAsync(idOrSlug, cancellationToken);
                    return context is null ? Results.NotFound() : Results.Ok(context);
                })
            .RequireAuthorization(CompanionPermissions.ProjectsRead);
        projects.MapPut(
                "/{idOrSlug}/context",
                async (string idOrSlug, UpdateContextRequest request, ClaimsPrincipal user, ProjectService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        return Results.Ok(await service.UpdateContextAsync(
                            idOrSlug, userId, clientId, request.ExpectedRevisionId, request.Title, request.Body, cancellationToken));
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                    catch (ConflictException ex)
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            title: ex.Message,
                            extensions: ex.Details is null ? null : new Dictionary<string, object?> { ["current"] = ex.Details });
                    }
                })
            .RequireAuthorization(CompanionPermissions.ProjectsManage);

        DocumentEndpoints.Map(endpoints);
        IssueEndpoints.Map(endpoints);

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

public sealed record CreateProjectRequest([Required, StringLength(200)] string Name);

public sealed record UpdateContextRequest(
    [Required] Guid ExpectedRevisionId,
    [Required] string Body,
    [StringLength(500)] string? Title);

public static class ModuleRegistry
{
    public static IReadOnlyList<IEnterpriseModule> Production { get; } = [new CompanionModule()];
}
