using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerResourceType]
public sealed class ProjectContextResources(ProjectService projects)
{
    [McpServerResource(
        UriTemplate = "companion://projects/{slug}/context",
        Name = "project-context",
        MimeType = "text/markdown"),
     Description("Read-only Project Context for a Project slug.")]
    public async Task<string> ProjectContext(
        [Description("Project slug")] string slug,
        CancellationToken cancellationToken)
    {
        var context = await projects.GetContextAsync(slug, cancellationToken);
        return context?.Body ?? throw new KeyNotFoundException();
    }

    [McpServerResource(
        UriTemplate = "companion://current-project/context",
        Name = "current-project-context",
        MimeType = "text/markdown"),
     Description("Read-only Project Context for the owner's default (oldest active) Project.")]
    public async Task<string> CurrentProjectContext(CancellationToken cancellationToken)
    {
        var context = await projects.GetCurrentContextAsync(cancellationToken);
        return context?.Body ?? throw new KeyNotFoundException();
    }
}
