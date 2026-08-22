using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class InboxTools(InboxService inbox, IHttpContextAccessor accessor)
{
    [McpServerTool(Name = "inbox_capture"), Description("Capture unclassified Inbox text. No Project is required.")]
    public Task<object> Capture([Description("Unclassified thought")] string text) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (InboxItemDto?)await inbox.CaptureAsync(userId, userId, clientId, text, CancellationToken.None);
        });

    [McpServerTool(Name = "inbox_list"), Description("List Inbox Items. Defaults to Open.")]
    public Task<IReadOnlyList<InboxItemDto>> List([Description("Open, Processed, or Archived")] string? status = null)
    {
        InboxStatus? filter = Enum.TryParse<InboxStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;
        return inbox.ListAsync(filter, CancellationToken.None);
    }

    [McpServerTool(Name = "inbox_process"), Description("Create or attach a Document or Issue and keep provenance. Does not classify automatically.")]
    public Task<object> Process(
        [Description("Inbox Item id")] string id,
        [Description("Optional title for a created target")] string? title = null,
        [Description("Project id when creating an Issue")] string? projectId = null,
        [Description("Create a new Document from the Inbox text")] bool createDocument = false,
        [Description("Create a new Issue from the Inbox text")] bool createIssue = false,
        [Description("Existing Document id to attach")] string? documentId = null,
        [Description("Existing Issue id to attach")] string? issueId = null) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (InboxItemDto?)await inbox.ProcessAsync(
                Guid.Parse(id),
                userId,
                userId,
                clientId,
                title,
                ParseGuid(projectId),
                createDocument,
                createIssue,
                ParseGuid(documentId),
                ParseGuid(issueId),
                CancellationToken.None);
        });

    [McpServerTool(Name = "inbox_archive"), Description("Dismiss an Inbox Item without creating a target.")]
    public Task<object> Archive([Description("Inbox Item id")] string id) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (InboxItemDto?)await inbox.ArchiveAsync(Guid.Parse(id), userId, clientId, CancellationToken.None);
        });

    private async Task<object> Invoke(Func<Task<InboxItemDto?>> action)
    {
        try
        {
            var result = await action();
            return result is null ? new { error = "not found" } : result;
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    private (string UserId, Guid? ClientId) Actor() =>
        ProjectService.ActorFrom(
            accessor.HttpContext?.User
            ?? throw new InvalidOperationException("Authenticated owner required."));

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
