using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record InboxItemDto(
    Guid Id,
    string Text,
    InboxStatus Status,
    Guid? DocumentId,
    Guid? IssueId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? ArchivedAt);

public sealed class InboxService(
    EnterpriseDbContext db,
    DocumentService documents,
    IssueService issues,
    ActivityService activity,
    TimeProvider time)
{
    public async Task<InboxItemDto> CaptureAsync(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ProtocolException("Inbox capture requires text.");
        }

        var now = time.GetUtcNow();
        var item = new InboxItem
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Text = text,
            Status = InboxStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<InboxItem>().Add(item);
        AddActivity(ownerUserId, actorUserId, aiClientId, "captured", item, "Captured inbox item");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<InboxItemDto>> ListAsync(InboxStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Set<InboxItem>().AsQueryable();
        query = status is InboxStatus filter
            ? query.Where(item => item.Status == filter)
            : query.Where(item => item.Status == InboxStatus.Open);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<InboxItemDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Set<InboxItem>().FirstOrDefaultAsync(inbox => inbox.Id == id, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<InboxItemDto> ProcessAsync(
        Guid id,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string? title,
        Guid? projectId,
        bool createDocument,
        bool createIssue,
        Guid? documentId,
        Guid? issueId,
        CancellationToken cancellationToken)
    {
        var item = await RequireOpenAsync(id, cancellationToken);
        var actions = (createDocument ? 1 : 0)
            + (createIssue ? 1 : 0)
            + (documentId is not null ? 1 : 0)
            + (issueId is not null ? 1 : 0);
        if (actions != 1)
        {
            throw new ProtocolException("Process must create or attach exactly one Document or Issue.");
        }

        if (createDocument)
        {
            var document = await documents.CreateAsync(
                ownerUserId,
                actorUserId,
                aiClientId,
                ResolveTitle(title, item.Text),
                item.Text,
                folderId: null,
                templateId: null,
                slug: null,
                projectId is Guid link ? [link] : null,
                tags: null,
                cancellationToken);
            item.DocumentId = document.Id;
        }
        else if (createIssue)
        {
            if (projectId is not Guid project)
            {
                throw new ProtocolException("Creating an Issue from Inbox requires a Project.");
            }

            var issue = await issues.CreateAsync(
                project,
                ownerUserId,
                actorUserId,
                aiClientId,
                ResolveTitle(title, item.Text),
                item.Text,
                IssueStatus.Backlog,
                IssuePriority.Normal,
                parentIssueId: null,
                cancellationToken);
            item.IssueId = issue.Id;
        }
        else if (documentId is Guid existingDocument)
        {
            _ = await db.Set<Document>().FirstOrDefaultAsync(
                    document => document.Id == existingDocument && document.ArchivedAt == null,
                    cancellationToken)
                ?? throw new KeyNotFoundException();
            item.DocumentId = existingDocument;
        }
        else
        {
            _ = await db.Set<Issue>().FirstOrDefaultAsync(
                    issue => issue.Id == issueId && issue.ArchivedAt == null,
                    cancellationToken)
                ?? throw new KeyNotFoundException();
            item.IssueId = issueId;
        }

        item.Status = InboxStatus.Processed;
        item.ProcessedAt = time.GetUtcNow();
        item.UpdatedAt = item.ProcessedAt.Value;
        AddActivity(item.OwnerUserId, actorUserId, aiClientId, "processed", item, "Processed inbox item");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<InboxItemDto> ArchiveAsync(
        Guid id,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var item = await RequireOpenAsync(id, cancellationToken);
        var now = time.GetUtcNow();
        item.Status = InboxStatus.Archived;
        item.ArchivedAt = now;
        item.UpdatedAt = now;
        AddActivity(item.OwnerUserId, actorUserId, aiClientId, "archived", item, "Archived inbox item");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<InboxItemDto> ReopenAsync(
        Guid id,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var item = await db.Set<InboxItem>().FirstOrDefaultAsync(inbox => inbox.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (item.Status == InboxStatus.Open)
        {
            throw new ProtocolException("Only Processed or Archived Inbox Items can be reopened.");
        }

        item.Status = InboxStatus.Open;
        item.ArchivedAt = null;
        item.UpdatedAt = time.GetUtcNow();
        AddActivity(item.OwnerUserId, actorUserId, aiClientId, "reopened", item, "Reopened inbox item");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    private async Task<InboxItem> RequireOpenAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Set<InboxItem>().FirstOrDefaultAsync(inbox => inbox.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (item.Status != InboxStatus.Open)
        {
            throw new ProtocolException("Only Open Inbox Items can be processed or archived.");
        }

        return item;
    }

    private static string ResolveTitle(string? title, string text)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        var first = text.Split('\n', 2)[0].Trim();
        if (string.IsNullOrWhiteSpace(first))
        {
            return "Untitled";
        }

        return first.Length <= 500 ? first : first[..500];
    }

    private void AddActivity(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string actionType,
        InboxItem item,
        string summary)
    {
        activity.Add(ownerUserId, actorUserId, aiClientId, actionType, "InboxItem", item.Id, projectId: null, summary);
    }

    private static InboxItemDto ToDto(InboxItem item) =>
        new(
            item.Id,
            item.Text,
            item.Status,
            item.DocumentId,
            item.IssueId,
            item.CreatedAt,
            item.ProcessedAt,
            item.ArchivedAt);
}
