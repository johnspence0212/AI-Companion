using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class SearchTools(SearchService search)
{
    [McpServerTool(Name = "search_query"), Description("Grouped full-text search over Projects, Documents, Issues, and Activity. Indexes exact Markdown including fenced code.")]
    public Task<SearchResultsDto> Query(
        [Description("Search text")] string q,
        [Description("Optional entity type: project, document, issue, or activity")] string? type = null,
        [Description("Optional Project id")] string? projectId = null,
        [Description("Optional Tag name")] string? tag = null,
        [Description("Include archived records")] bool archived = false) =>
        search.SearchAsync(q, type, ParseGuid(projectId), tag, archived, CancellationToken.None);

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
