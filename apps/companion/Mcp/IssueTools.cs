using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class IssueTools(IssueService issues, IHttpContextAccessor accessor)
{
    [McpServerTool(Name = "issues_list"), Description("List active Issues in a Project.")]
    public Task<IReadOnlyList<IssueDto>> List([Description("Project id")] string projectId) =>
        issues.ListAsync(Guid.Parse(projectId), CancellationToken.None);

    [McpServerTool(Name = "issues_get"), Description("Get an Issue by id.")]
    public Task<IssueDto?> Get([Description("Issue id")] string id) =>
        issues.GetAsync(Guid.Parse(id), CancellationToken.None);

    [McpServerTool(Name = "issues_create"), Description("Create an Issue. Defaults to Backlog and Normal priority.")]
    public Task<IssueDto> Create(
        [Description("Project id")] string projectId,
        [Description("Issue title")] string title,
        [Description("Optional Markdown description")] string? description = null,
        [Description("Backlog or Ready")] string? status = null,
        [Description("None, Low, Normal, High, or Urgent")] string? priority = null,
        [Description("Optional parent issue id")] string? parentIssueId = null)
    {
        var (userId, clientId) = Actor();
        return issues.CreateAsync(
            Guid.Parse(projectId),
            userId,
            userId,
            clientId,
            title,
            description,
            ParseStatus(status) ?? IssueStatus.Backlog,
            ParsePriority(priority) ?? IssuePriority.Normal,
            ParseGuid(parentIssueId),
            CancellationToken.None);
    }

    [McpServerTool(Name = "issues_claim"), Description("Claim an unassigned Issue without changing status.")]
    public Task<object> Claim(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (IssueDto?)await issues.ClaimAsync(Guid.Parse(id), expectedVersion, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_start"), Description("Assign if needed and move a Ready, unblocked Issue to Active.")]
    public Task<object> Start(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (IssueDto?)await issues.StartAsync(Guid.Parse(id), expectedVersion, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_block"), Description("Move an Issue to Blocked with a Markdown reason.")]
    public Task<object> Block(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion,
        [Description("Markdown blocked reason")] string reason) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (IssueDto?)await issues.BlockAsync(Guid.Parse(id), expectedVersion, reason, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_complete"), Description("Move an Issue to Done with an attributed Markdown resolution.")]
    public Task<object> Complete(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion,
        [Description("Markdown resolution")] string resolution) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (IssueDto?)await issues.CompleteAsync(Guid.Parse(id), expectedVersion, resolution, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_cancel"), Description("Move an Issue to Canceled with a Markdown reason.")]
    public Task<object> Cancel(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion,
        [Description("Markdown cancel reason")] string reason) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (IssueDto?)await issues.CancelAsync(Guid.Parse(id), expectedVersion, reason, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_reopen"), Description("Reopen a Done or Canceled Issue to Ready, keeping the assignee.")]
    public Task<object> Reopen(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (IssueDto?)await issues.ReopenAsync(Guid.Parse(id), expectedVersion, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_archive"), Description("Archive a terminal Issue whose children are also terminal.")]
    public Task<object> Archive(
        [Description("Issue id")] string id,
        [Description("Expected current version")] int expectedVersion) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return await issues.ArchiveAsync(Guid.Parse(id), expectedVersion, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "issues_get_next"), Description("Read the next Issue for the caller without claiming it.")]
    public Task<IssueDto?> GetNext([Description("Project id")] string projectId)
    {
        var (userId, clientId) = Actor();
        return issues.GetNextAsync(Guid.Parse(projectId), userId, clientId, CancellationToken.None);
    }

    [McpServerTool(Name = "issues_claim_next"), Description("Atomically claim the next Ready Issue and leave it Ready.")]
    public Task<IssueDto?> ClaimNext([Description("Project id")] string projectId)
    {
        var (userId, clientId) = Actor();
        return issues.ClaimNextAsync(Guid.Parse(projectId), userId, clientId, CancellationToken.None);
    }

    [McpServerTool(Name = "issues_start_next"), Description("Return the caller's Active Issue, or start the next Ready Issue.")]
    public Task<IssueDto?> StartNext([Description("Project id")] string projectId)
    {
        var (userId, clientId) = Actor();
        return issues.StartNextAsync(Guid.Parse(projectId), userId, clientId, CancellationToken.None);
    }

    private async Task<object> Invoke(Func<Task<IssueDto?>> action)
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

    private static IssueStatus? ParseStatus(string? value) =>
        Enum.TryParse<IssueStatus>(value, ignoreCase: true, out var status) ? status : null;

    private static IssuePriority? ParsePriority(string? value) =>
        Enum.TryParse<IssuePriority>(value, ignoreCase: true, out var priority) ? priority : null;
}
