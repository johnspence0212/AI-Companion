using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class ProjectTools(ProjectService projects, IHttpContextAccessor accessor)
{
    [McpServerTool(Name = "projects_list"), Description("List the owner's active Projects.")]
    public Task<IReadOnlyList<ProjectDto>> List(CancellationToken cancellationToken) =>
        projects.ListAsync(cancellationToken);

    [McpServerTool(Name = "projects_create"), Description("Create a Project with an empty Project Context.")]
    public Task<ProjectDto> Create(
        [Description("Project name")] string name,
        CancellationToken cancellationToken)
    {
        var (userId, clientId) = Actor();
        return projects.CreateAsync(userId, userId, clientId, name, cancellationToken);
    }

    [McpServerTool(Name = "projects_get"), Description("Get a Project by id or slug.")]
    public Task<ProjectDto?> Get(
        [Description("Project id or slug")] string idOrSlug,
        CancellationToken cancellationToken) =>
        projects.GetAsync(idOrSlug, cancellationToken);

    [McpServerTool(Name = "projects_archive"), Description("Archive a Project. Archive is reversible and does not delete content.")]
    public Task<ProjectDto?> Archive(
        [Description("Project id")] string id,
        CancellationToken cancellationToken)
    {
        var (userId, clientId) = Actor();
        return projects.ArchiveAsync(Guid.Parse(id), userId, clientId, cancellationToken);
    }

    [McpServerTool(Name = "context_get"), Description("Read Project Context by Project id or slug.")]
    public Task<ProjectContextDto?> GetContext(
        [Description("Project id or slug")] string idOrSlug,
        CancellationToken cancellationToken) =>
        projects.GetContextAsync(idOrSlug, cancellationToken);

    [McpServerTool(Name = "context_update"), Description("Replace Project Context Markdown by Project id or slug.")]
    public async Task<object> UpdateContext(
        [Description("Project id or slug")] string idOrSlug,
        [Description("Expected current revision id")] string expectedRevisionId,
        [Description("Full Markdown body")] string body,
        [Description("Optional Context title")] string? title = null)
    {
        try
        {
            if (!Guid.TryParse(expectedRevisionId, out var revisionId))
            {
                return new { error = "expectedRevisionId must be a GUID." };
            }

            var (userId, clientId) = Actor();
            return await projects.UpdateContextAsync(
                idOrSlug, userId, clientId, revisionId, title, body, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return new { error = ex.ToString() };
        }
    }

    private (string UserId, Guid? ClientId) Actor() =>
        ProjectService.ActorFrom(
            accessor.HttpContext?.User
            ?? throw new InvalidOperationException("Authenticated owner required."));
}
