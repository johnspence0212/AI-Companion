using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class ActivityTools(ActivityService activity)
{
    [McpServerTool(Name = "activity_list"), Description("List recent Activity. Summaries are short pointers, never full Markdown.")]
    public Task<IReadOnlyList<ActivityDto>> List(
        [Description("Optional Project id")] string? projectId = null,
        [Description("Optional record type such as Issue or Document")] string? recordType = null,
        [Description("Optional Session id")] string? sessionId = null) =>
        activity.ListAsync(ParseGuid(projectId), recordType, ParseGuid(sessionId), CancellationToken.None);

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
