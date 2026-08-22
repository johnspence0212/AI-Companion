using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record DailyItemDto(
    Guid Id,
    DateOnly Date,
    int Rank,
    Guid? IssueId,
    string? IssueTitle,
    IssueStatus? IssueStatus,
    string? CustomText,
    DateTimeOffset? CompletedAt);

public sealed record DailyBlockedDto(
    Guid IssueId,
    Guid ProjectId,
    string Title,
    IssueStatus Status,
    string? BlockedReason);

public sealed record DailyDto(
    DateOnly Date,
    IReadOnlyList<DailyItemDto> Items,
    IReadOnlyList<DailyItemDto> Carryover,
    IReadOnlyList<DailyBlockedDto> Blocked);

public sealed class DailyService(EnterpriseDbContext db, TimeProvider time)
{
    public const int CarryoverDays = 7;

    public async Task<DailyDto> GetAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var target = date ?? DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var carryoverStart = target.AddDays(-CarryoverDays);
        var items = await db.Set<DailyItem>()
            .Include(item => item.Issue)
            .Where(item => item.ArchivedAt == null && item.Date <= target && item.Date >= carryoverStart)
            .OrderBy(item => item.Date)
            .ThenBy(item => item.Rank)
            .ToListAsync(cancellationToken);

        var today = items
            .Where(item => item.Date == target)
            .Select(ToDto)
            .ToList();
        var carryover = items
            .Where(item => item.Date < target && item.CompletedAt == null)
            .Select(ToDto)
            .ToList();

        return new DailyDto(target, today, carryover, await ListBlockedAsync(cancellationToken));
    }

    public async Task<DailyItemDto> AddIssueAsync(
        DateOnly date,
        Guid issueId,
        int? rank,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issue = await db.Set<Issue>().FirstOrDefaultAsync(item => item.Id == issueId && item.ArchivedAt == null, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (await db.Set<DailyItem>().AnyAsync(
                item => item.Date == date && item.IssueId == issueId && item.ArchivedAt == null,
                cancellationToken))
        {
            throw new ProtocolException("The same Issue appears at most once per date.");
        }

        return await AddAsync(date, issue.Id, customText: null, rank, ownerUserId, actorUserId, aiClientId, issue, cancellationToken);
    }

    public async Task<DailyItemDto> AddItemAsync(
        DateOnly date,
        string customText,
        int? rank,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customText))
        {
            throw new ProtocolException("Custom Daily Items require text.");
        }

        return await AddAsync(date, issueId: null, customText.Trim(), rank, ownerUserId, actorUserId, aiClientId, issue: null, cancellationToken);
    }

    public async Task<DailyItemDto> CompleteAsync(
        Guid id,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var item = await RequireAsync(id, cancellationToken);
        if (item.CompletedAt is null)
        {
            item.CompletedAt = time.GetUtcNow();
            item.UpdatedAt = item.CompletedAt.Value;
            AddActivity(item.OwnerUserId, actorUserId, aiClientId, "completed", item, "Completed daily item");
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(item);
    }

    public async Task<DailyItemDto?> RemoveAsync(
        Guid id,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var item = await db.Set<DailyItem>()
            .Include(daily => daily.Issue)
            .FirstOrDefaultAsync(daily => daily.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.ArchivedAt = time.GetUtcNow();
        item.UpdatedAt = item.ArchivedAt.Value;
        AddActivity(item.OwnerUserId, actorUserId, aiClientId, "removed", item, "Removed daily item");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private async Task<DailyItemDto> AddAsync(
        DateOnly date,
        Guid? issueId,
        string? customText,
        int? rank,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        Issue? issue,
        CancellationToken cancellationToken)
    {
        if (issueId is not null && !string.IsNullOrWhiteSpace(customText))
        {
            throw new ProtocolException("A Daily Item is either one Issue or custom text, never both.");
        }

        var resolvedRank = rank ?? await NextRankAsync(date, cancellationToken);
        var now = time.GetUtcNow();
        var item = new DailyItem
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Date = date,
            Rank = resolvedRank,
            IssueId = issueId,
            Issue = issue,
            CustomText = customText,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<DailyItem>().Add(item);
        AddActivity(ownerUserId, actorUserId, aiClientId, "added", item, issue is null ? "Added custom daily item" : "Added issue to daily");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private async Task<DailyItem> RequireAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Set<DailyItem>()
            .Include(item => item.Issue)
            .FirstOrDefaultAsync(item => item.Id == id && item.ArchivedAt == null, cancellationToken)
        ?? throw new KeyNotFoundException();

    private async Task<int> NextRankAsync(DateOnly date, CancellationToken cancellationToken) =>
        (await db.Set<DailyItem>()
            .Where(item => item.Date == date && item.ArchivedAt == null)
            .Select(item => (int?)item.Rank)
            .MaxAsync(cancellationToken) ?? -1) + 1;

    private async Task<IReadOnlyList<DailyBlockedDto>> ListBlockedAsync(CancellationToken cancellationToken)
    {
        var issues = await db.Set<Issue>()
            .Include(issue => issue.Blockers)
            .ThenInclude(link => link.BlockerIssue)
            .Where(issue => issue.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        return issues
            .Where(IsEffectivelyBlocked)
            .OrderByDescending(issue => issue.Priority)
            .ThenBy(issue => issue.Title)
            .Select(issue => new DailyBlockedDto(
                issue.Id,
                issue.ProjectId,
                issue.Title,
                issue.Status,
                issue.BlockedReason))
            .ToList();
    }

    private static bool IsEffectivelyBlocked(Issue issue)
    {
        if (issue.Status == IssueStatus.Blocked)
        {
            return true;
        }

        return issue.Blockers.Any(link =>
            link.BlockerIssue is { Status: not (IssueStatus.Done or IssueStatus.Canceled) });
    }

    private void AddActivity(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string actionType,
        DailyItem item,
        string summary)
    {
        var now = time.GetUtcNow();
        db.Set<Activity>().Add(new Activity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            OccurredAt = now,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            ActionType = actionType,
            RecordType = "DailyItem",
            RecordId = item.Id,
            ProjectId = item.Issue?.ProjectId,
            Summary = summary,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static DailyItemDto ToDto(DailyItem item) =>
        new(
            item.Id,
            item.Date,
            item.Rank,
            item.IssueId,
            item.Issue?.Title,
            item.Issue?.Status,
            item.CustomText,
            item.CompletedAt);
}
