using System.ComponentModel;
using System.Globalization;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class DailyTools(DailyService daily, IHttpContextAccessor accessor)
{
    [McpServerTool(Name = "daily_get"), Description("Get the Owner's Daily list for a user-local date, plus 7-day carryover and derived blocked Issues.")]
    public Task<DailyDto> Get([Description("User-local date as YYYY-MM-DD")] string? date = null) =>
        daily.GetAsync(ParseDate(date), CancellationToken.None);

    [McpServerTool(Name = "daily_add_issue"), Description("Add an Issue reference to a Daily list. Completing Daily later does not complete the Issue.")]
    public Task<object> AddIssue(
        [Description("User-local date as YYYY-MM-DD")] string date,
        [Description("Issue id")] string issueId,
        [Description("Optional rank")] int? rank = null) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (DailyItemDto?)await daily.AddIssueAsync(
                RequireDate(date), Guid.Parse(issueId), rank, userId, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "daily_add_item"), Description("Add a custom-text Daily Item that does not belong to an Issue.")]
    public Task<object> AddItem(
        [Description("User-local date as YYYY-MM-DD")] string date,
        [Description("Custom Daily text")] string customText,
        [Description("Optional rank")] int? rank = null) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (DailyItemDto?)await daily.AddItemAsync(
                RequireDate(date), customText, rank, userId, userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "daily_complete_item"), Description("Complete a Daily Item without changing the referenced Issue.")]
    public Task<object> Complete([Description("Daily Item id")] string id) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return (DailyItemDto?)await daily.CompleteAsync(Guid.Parse(id), userId, clientId, CancellationToken.None);
        });

    [McpServerTool(Name = "daily_remove_item"), Description("Remove a Daily Item. The referenced Issue is unchanged.")]
    public Task<object> Remove([Description("Daily Item id")] string id) =>
        Invoke(async () =>
        {
            var (userId, clientId) = Actor();
            return await daily.RemoveAsync(Guid.Parse(id), userId, clientId, CancellationToken.None);
        });

    private async Task<object> Invoke(Func<Task<DailyItemDto?>> action)
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

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static DateOnly RequireDate(string value) =>
        ParseDate(value) ?? throw new ProtocolException("date must be YYYY-MM-DD.");
}
