using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed class ProtocolException(string message) : Exception(message);

public sealed record IssueDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentIssueId,
    string Title,
    string? Description,
    IssueStatus Status,
    IssuePriority Priority,
    int Rank,
    string? AssigneeUserId,
    Guid? AssigneeAiClientId,
    string? BlockedReason,
    string? Resolution,
    int Version,
    bool EffectivelyBlocked,
    DateTimeOffset? ArchivedAt);

public sealed class IssueService(EnterpriseDbContext db, TimeProvider time)
{
    public async Task<IssueDto> CreateAsync(
        Guid projectId,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string title,
        string? description,
        IssueStatus status,
        IssuePriority priority,
        Guid? parentIssueId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ProtocolException("Title is required.");
        }

        if (status is not (IssueStatus.Backlog or IssueStatus.Ready))
        {
            throw new ProtocolException("New Issues start as Backlog or Ready.");
        }

        var project = await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (parentIssueId is Guid parentId)
        {
            await EnsureParentAsync(projectId, parentId, childId: null, cancellationToken);
        }

        var rank = await NextRankAsync(projectId, status, cancellationToken);
        var now = time.GetUtcNow();
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            ProjectId = project.Id,
            ParentIssueId = parentIssueId,
            Title = title.Trim(),
            Description = description,
            Status = status,
            Priority = priority,
            Rank = rank,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<Issue>().Add(issue);
        AddActivity(ownerUserId, actorUserId, aiClientId, "created", issue.Id, project.Id, $"Created issue {issue.Title}");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, effectivelyBlocked: false);
    }

    public async Task<IReadOnlyList<IssueDto>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var issues = await RequireProjectIssuesAsync(projectId, cancellationToken);
        return issues
            .Where(issue => issue.ArchivedAt == null)
            .OrderBy(issue => issue.Status)
            .ThenBy(issue => issue.Rank)
            .Select(issue => ToDto(issue, IsEffectivelyBlocked(issue, issues)))
            .ToList();
    }

    public async Task<IssueDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var issue = await LoadIssueAsync(id, cancellationToken);
        if (issue is null)
        {
            return null;
        }

        var siblings = await LoadProjectIssuesAsync(issue.ProjectId, cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, siblings));
    }

    public async Task<IssueDto> StartAsync(
        Guid id,
        int expectedVersion,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        if (issue.Status != IssueStatus.Ready)
        {
            throw new ProtocolException("Only Ready Issues can be started.");
        }

        if (IsEffectivelyBlocked(issue, issues))
        {
            throw new ProtocolException("Effectively blocked Issues cannot be started.");
        }

        AssignIfUnassigned(issue, actorUserId, aiClientId);
        EnsureCallerOwns(issue, actorUserId, aiClientId);
        issue.Status = IssueStatus.Active;
        Touch(issue, actorUserId, aiClientId, "started", "Started issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, false);
    }

    public async Task<IssueDto> BlockAsync(
        Guid id,
        int expectedVersion,
        string reason,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        EnsureNonterminal(issue);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ProtocolException("Blocking requires a Markdown reason.");
        }

        issue.Status = IssueStatus.Blocked;
        issue.BlockedReason = reason;
        Touch(issue, actorUserId, aiClientId, "blocked", "Blocked issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, true);
    }

    public async Task<IssueDto> UnblockAsync(
        Guid id,
        int expectedVersion,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        if (issue.Status != IssueStatus.Blocked)
        {
            throw new ProtocolException("Only Blocked Issues can be unblocked.");
        }

        issue.Status = IssueStatus.Ready;
        issue.BlockedReason = null;
        Touch(issue, actorUserId, aiClientId, "unblocked", "Unblocked issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, issues));
    }

    public async Task<IssueDto> CompleteAsync(
        Guid id,
        int expectedVersion,
        string resolution,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        EnsureNonterminal(issue);
        if (string.IsNullOrWhiteSpace(resolution))
        {
            throw new ProtocolException("Done requires a Markdown resolution.");
        }

        if (IsEffectivelyBlocked(issue, issues))
        {
            throw new ProtocolException("Effectively blocked Issues cannot become Done.");
        }

        if (HasOpenChildren(issue, issues))
        {
            throw new ProtocolException("Parents cannot become Done until every child is Done or Canceled.");
        }

        issue.Status = IssueStatus.Done;
        issue.Resolution = resolution;
        Touch(issue, actorUserId, aiClientId, "completed", "Completed issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, false);
    }

    public async Task<IssueDto> CancelAsync(
        Guid id,
        int expectedVersion,
        string reason,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        EnsureNonterminal(issue);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ProtocolException("Canceled requires a Markdown reason.");
        }

        if (HasOpenChildren(issue, issues))
        {
            throw new ProtocolException("Parents cannot be canceled until every child is Done or Canceled.");
        }

        issue.Status = IssueStatus.Canceled;
        issue.Resolution = reason;
        Touch(issue, actorUserId, aiClientId, "canceled", "Canceled issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, issues));
    }

    public async Task<IssueDto> ReopenAsync(
        Guid id,
        int expectedVersion,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        if (issue.Status is not (IssueStatus.Done or IssueStatus.Canceled))
        {
            throw new ProtocolException("Only Done or Canceled Issues can be reopened.");
        }

        issue.Status = IssueStatus.Ready;
        Touch(issue, actorUserId, aiClientId, "reopened", "Reopened issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, issues));
    }

    public async Task<IssueDto> ClaimAsync(
        Guid id,
        int expectedVersion,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        EnsureNonterminal(issue);
        if (IsAssigned(issue) && !CallerOwns(issue, actorUserId, aiClientId))
        {
            throw new ProtocolException("Claims cannot be stolen.");
        }

        AssignIfUnassigned(issue, actorUserId, aiClientId);
        Touch(issue, actorUserId, aiClientId, "claimed", "Claimed issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, issues));
    }

    public async Task<IssueDto> MoveAsync(
        Guid id,
        int expectedVersion,
        IssueStatus status,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        return status switch
        {
            IssueStatus.Active => await StartAsync(id, expectedVersion, actorUserId, aiClientId, cancellationToken),
            IssueStatus.Done => throw new ProtocolException("Use complete with a resolution."),
            IssueStatus.Canceled => throw new ProtocolException("Use cancel with a reason."),
            IssueStatus.Blocked => throw new ProtocolException("Use block with a reason."),
            _ => await SetStatusAsync(id, expectedVersion, status, actorUserId, aiClientId, cancellationToken)
        };
    }

    public async Task<IssueDto> AddBlockerAsync(
        Guid id,
        Guid blockerId,
        int expectedVersion,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        var blocker = issues.FirstOrDefault(item => item.Id == blockerId)
            ?? throw new KeyNotFoundException();
        if (blocker.ProjectId != issue.ProjectId)
        {
            throw new ProtocolException("Blockers must stay in the same Project.");
        }

        if (WouldCycle(issues, id, blockerId))
        {
            throw new ProtocolException("Blockers must be cycle-free.");
        }

        if (!issue.Blockers.Any(link => link.BlockerIssueId == blockerId))
        {
            issue.Blockers.Add(new IssueBlocker { IssueId = id, BlockerIssueId = blockerId });
        }

        Touch(issue, actorUserId, aiClientId, "blocker-added", "Added blocker");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, issues));
    }

    public async Task<IssueDto?> ArchiveAsync(
        Guid id,
        int expectedVersion,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.FirstOrDefault(item => item.Id == id);
        if (issue is null)
        {
            return null;
        }

        EnsureVersion(issue, expectedVersion);
        if (issue.Status is not (IssueStatus.Done or IssueStatus.Canceled))
        {
            throw new ProtocolException("Only Done or Canceled Issues may be archived.");
        }

        if (HasOpenChildren(issue, issues))
        {
            throw new ProtocolException("Parents cannot be archived until every child is Done or Canceled.");
        }

        issue.ArchivedAt = time.GetUtcNow();
        Touch(issue, actorUserId, aiClientId, "archived", "Archived issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, false);
    }

    public async Task<IssueDto?> GetNextAsync(
        Guid projectId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await RequireProjectIssuesAsync(projectId, cancellationToken);
        var selected = SelectNext(issues, actorUserId, aiClientId, includeActive: true);
        return selected is null ? null : ToDto(selected, IsEffectivelyBlocked(selected, issues));
    }

    public async Task<IssueDto?> ClaimNextAsync(
        Guid projectId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await RequireProjectIssuesAsync(projectId, cancellationToken);
        var selected = SelectNext(issues, actorUserId, aiClientId, includeActive: false);
        if (selected is null)
        {
            return null;
        }

        AssignIfUnassigned(selected, actorUserId, aiClientId);
        Touch(selected, actorUserId, aiClientId, "claimed", "Claimed next issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(selected, IsEffectivelyBlocked(selected, issues));
    }

    public async Task<IssueDto?> StartNextAsync(
        Guid projectId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await RequireProjectIssuesAsync(projectId, cancellationToken);
        var active = SelectNext(issues, actorUserId, aiClientId, includeActive: true);
        if (active is { Status: IssueStatus.Active })
        {
            return ToDto(active, false);
        }

        var ready = SelectNext(issues, actorUserId, aiClientId, includeActive: false);
        if (ready is null)
        {
            return null;
        }

        AssignIfUnassigned(ready, actorUserId, aiClientId);
        ready.Status = IssueStatus.Active;
        Touch(ready, actorUserId, aiClientId, "started", "Started next issue");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(ready, false);
    }

    private async Task<IssueDto> SetStatusAsync(
        Guid id,
        int expectedVersion,
        IssueStatus status,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var issues = await LoadForMutationAsync(id, cancellationToken);
        var issue = issues.First(item => item.Id == id);
        EnsureVersion(issue, expectedVersion);
        EnsureNonterminal(issue);
        issue.Status = status;
        if (status != IssueStatus.Blocked)
        {
            issue.BlockedReason = null;
        }

        Touch(issue, actorUserId, aiClientId, "moved", $"Moved issue to {status}");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(issue, IsEffectivelyBlocked(issue, issues));
    }

    private async Task<List<Issue>> RequireProjectIssuesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await db.Set<Project>().AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw new KeyNotFoundException();
        }

        return await LoadProjectIssuesAsync(projectId, cancellationToken);
    }

    private async Task<List<Issue>> LoadProjectIssuesAsync(Guid projectId, CancellationToken cancellationToken) =>
        await db.Set<Issue>()
            .Include(issue => issue.Blockers)
            .ThenInclude(link => link.BlockerIssue)
            .Include(issue => issue.Children)
            .Where(issue => issue.ProjectId == projectId)
            .ToListAsync(cancellationToken);

    private async Task<List<Issue>> LoadForMutationAsync(Guid id, CancellationToken cancellationToken)
    {
        var issue = await db.Set<Issue>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        return await LoadProjectIssuesAsync(issue.ProjectId, cancellationToken);
    }

    private Task<Issue?> LoadIssueAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<Issue>()
            .Include(issue => issue.Blockers)
            .ThenInclude(link => link.BlockerIssue)
            .Include(issue => issue.Children)
            .FirstOrDefaultAsync(issue => issue.Id == id, cancellationToken);

    private async Task EnsureParentAsync(Guid projectId, Guid parentId, Guid? childId, CancellationToken cancellationToken)
    {
        var parent = await db.Set<Issue>().FirstOrDefaultAsync(item => item.Id == parentId, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (parent.ProjectId != projectId)
        {
            throw new ProtocolException("Parent and child must stay in the same Project.");
        }

        if (childId is Guid id && await WouldParentCycleAsync(id, parentId, cancellationToken))
        {
            throw new ProtocolException("Issue parents must be acyclic.");
        }
    }

    private async Task<bool> WouldParentCycleAsync(Guid childId, Guid parentId, CancellationToken cancellationToken)
    {
        var current = parentId;
        var seen = new HashSet<Guid> { childId };
        while (true)
        {
            if (!seen.Add(current))
            {
                return true;
            }

            var next = await db.Set<Issue>()
                .Where(item => item.Id == current)
                .Select(item => item.ParentIssueId)
                .FirstOrDefaultAsync(cancellationToken);
            if (next is null)
            {
                return false;
            }

            current = next.Value;
        }
    }

    private async Task<int> NextRankAsync(Guid projectId, IssueStatus status, CancellationToken cancellationToken) =>
        (await db.Set<Issue>()
            .Where(issue => issue.ProjectId == projectId && issue.Status == status)
            .Select(issue => (int?)issue.Rank)
            .MaxAsync(cancellationToken) ?? -1) + 1;

    private static Issue? SelectNext(
        IReadOnlyCollection<Issue> issues,
        string actorUserId,
        Guid? aiClientId,
        bool includeActive)
    {
        IEnumerable<Issue> candidates = issues.Where(issue => issue.ArchivedAt == null);
        if (includeActive)
        {
            var active = Order(candidates.Where(issue =>
                issue.Status == IssueStatus.Active && CallerOwns(issue, actorUserId, aiClientId)));
            var firstActive = active.FirstOrDefault();
            if (firstActive is not null)
            {
                return firstActive;
            }
        }

        return Order(candidates.Where(issue =>
                issue.Status == IssueStatus.Ready
                && !IsEffectivelyBlocked(issue, issues)
                && (!IsAssigned(issue) || CallerOwns(issue, actorUserId, aiClientId))))
            .FirstOrDefault();
    }

    private static IOrderedEnumerable<Issue> Order(IEnumerable<Issue> issues) =>
        issues
            .OrderByDescending(issue => issue.Priority)
            .ThenBy(issue => issue.Rank)
            .ThenBy(issue => issue.CreatedAt);

    private static bool IsEffectivelyBlocked(Issue issue, IReadOnlyCollection<Issue> projectIssues)
    {
        if (issue.Status == IssueStatus.Blocked)
        {
            return true;
        }

        var byId = projectIssues.ToDictionary(item => item.Id);
        return issue.Blockers.Any(link =>
            byId.TryGetValue(link.BlockerIssueId, out var blocker)
            && blocker.Status is not (IssueStatus.Done or IssueStatus.Canceled));
    }

    private static bool HasOpenChildren(Issue issue, IReadOnlyCollection<Issue> projectIssues) =>
        projectIssues.Any(child =>
            child.ParentIssueId == issue.Id
            && child.Status is not (IssueStatus.Done or IssueStatus.Canceled));

    private static bool WouldCycle(IReadOnlyCollection<Issue> issues, Guid issueId, Guid blockerId)
    {
        var edges = issues
            .SelectMany(issue => issue.Blockers.Select(link => (From: issue.Id, To: link.BlockerIssueId)))
            .Append((From: issueId, To: blockerId))
            .ToLookup(edge => edge.From, edge => edge.To);
        var seen = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(blockerId);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == issueId)
            {
                return true;
            }

            if (!seen.Add(current))
            {
                continue;
            }

            foreach (var next in edges[current])
            {
                stack.Push(next);
            }
        }

        return false;
    }

    private static void EnsureVersion(Issue issue, int expectedVersion)
    {
        if (issue.Version != expectedVersion)
        {
            throw new ConflictException(
                "Issue was updated by another writer.",
                new { currentVersion = issue.Version, status = issue.Status.ToString() });
        }
    }

    private static void EnsureNonterminal(Issue issue)
    {
        if (issue.Status is IssueStatus.Done or IssueStatus.Canceled)
        {
            throw new ProtocolException("Terminal Issues cannot be mutated this way.");
        }
    }

    private static bool IsAssigned(Issue issue) =>
        !string.IsNullOrWhiteSpace(issue.AssigneeUserId) || issue.AssigneeAiClientId is not null;

    private static bool CallerOwns(Issue issue, string actorUserId, Guid? aiClientId) =>
        aiClientId is Guid client
            ? issue.AssigneeAiClientId == client
            : issue.AssigneeUserId == actorUserId && issue.AssigneeAiClientId is null;

    private static void EnsureCallerOwns(Issue issue, string actorUserId, Guid? aiClientId)
    {
        if (!CallerOwns(issue, actorUserId, aiClientId))
        {
            throw new ProtocolException("The Issue is assigned to another actor.");
        }
    }

    private void AssignIfUnassigned(Issue issue, string actorUserId, Guid? aiClientId)
    {
        if (IsAssigned(issue))
        {
            return;
        }

        issue.AssigneeUserId = actorUserId;
        issue.AssigneeAiClientId = aiClientId;
        issue.ClaimedAt = time.GetUtcNow();
    }

    private void Touch(Issue issue, string actorUserId, Guid? aiClientId, string action, string summary)
    {
        issue.Version++;
        issue.UpdatedAt = time.GetUtcNow();
        AddActivity(issue.OwnerUserId, actorUserId, aiClientId, action, issue.Id, issue.ProjectId, summary);
    }

    private void AddActivity(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string actionType,
        Guid recordId,
        Guid projectId,
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
            RecordType = "Issue",
            RecordId = recordId,
            ProjectId = projectId,
            Summary = summary,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static IssueDto ToDto(Issue issue, bool effectivelyBlocked) =>
        new(
            issue.Id,
            issue.ProjectId,
            issue.ParentIssueId,
            issue.Title,
            issue.Description,
            issue.Status,
            issue.Priority,
            issue.Rank,
            issue.AssigneeUserId,
            issue.AssigneeAiClientId,
            issue.BlockedReason,
            issue.Resolution,
            issue.Version,
            effectivelyBlocked,
            issue.ArchivedAt);
}
