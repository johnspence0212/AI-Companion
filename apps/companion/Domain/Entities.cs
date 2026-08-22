using EnterpriseStarter.ModuleAbstractions;

namespace EnterpriseStarter.Companion.Domain;

public enum IssueStatus
{
    Backlog,
    Ready,
    Active,
    Blocked,
    Done,
    Canceled
}

public enum IssuePriority
{
    None,
    Low,
    Normal,
    High,
    Urgent
}

public enum InboxStatus
{
    Open,
    Processed,
    Archived
}

public enum SavedViewEntityType
{
    Projects,
    Documents,
    Issues,
    Activity
}

public abstract class OwnedRecord : IOwnedRecord
{
    public Guid Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public abstract class MutableOwnedRecord : OwnedRecord
{
    public DateTimeOffset? ArchivedAt { get; set; }
}

public sealed class Project : MutableOwnedRecord
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid ContextDocumentId { get; set; }
    public Document ContextDocument { get; set; } = null!;
    public int Version { get; set; } = 1;
    public ICollection<Issue> Issues { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<DocumentProject> DocumentLinks { get; set; } = [];
}

public sealed class Folder : MutableOwnedRecord
{
    public Guid? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rank { get; set; }
    public ICollection<Folder> Children { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
}

public sealed class Document : MutableOwnedRecord
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public Guid? FolderId { get; set; }
    public Folder? Folder { get; set; }
    public Guid? CurrentRevisionId { get; set; }
    public Revision? CurrentRevision { get; set; }
    public bool IsProjectContext { get; set; }
    public ICollection<Revision> Revisions { get; set; } = [];
    public ICollection<DocumentProject> ProjectLinks { get; set; } = [];
    public ICollection<DocumentTag> TagLinks { get; set; } = [];
}

public sealed class Revision : OwnedRecord
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public Guid? ActorAiClientId { get; set; }
    public string Kind { get; set; } = "save";
}

public sealed class Tag : MutableOwnedRecord
{
    public string Name { get; set; } = string.Empty;
    public ICollection<DocumentTag> DocumentLinks { get; set; } = [];
    public ICollection<IssueTag> IssueLinks { get; set; } = [];
}

public sealed class DocumentTemplate : MutableOwnedRecord
{
    public string Name { get; set; } = string.Empty;
    public string TitlePattern { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class DocumentProject
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}

public sealed class DocumentTag
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

public sealed class Issue : MutableOwnedRecord
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid? ParentIssueId { get; set; }
    public Issue? ParentIssue { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Backlog;
    public IssuePriority Priority { get; set; } = IssuePriority.None;
    public int Rank { get; set; }
    public string? AssigneeUserId { get; set; }
    public Guid? AssigneeAiClientId { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? BlockedReason { get; set; }
    public string? Resolution { get; set; }
    public int Version { get; set; } = 1;
    public ICollection<Issue> Children { get; set; } = [];
    public ICollection<IssueBlocker> Blockers { get; set; } = [];
    public ICollection<IssueBlocker> Blocking { get; set; } = [];
    public ICollection<IssueTag> TagLinks { get; set; } = [];
}

public sealed class IssueBlocker
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public Guid BlockerIssueId { get; set; }
    public Issue BlockerIssue { get; set; } = null!;
}

public sealed class IssueTag
{
    public Guid IssueId { get; set; }
    public Issue Issue { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

public sealed class DailyItem : MutableOwnedRecord
{
    public DateOnly Date { get; set; }
    public int Rank { get; set; }
    public Guid? IssueId { get; set; }
    public Issue? Issue { get; set; }
    public string? CustomText { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class InboxItem : MutableOwnedRecord
{
    public string Text { get; set; } = string.Empty;
    public InboxStatus Status { get; set; } = InboxStatus.Open;
    public Guid? DocumentId { get; set; }
    public Document? Document { get; set; }
    public Guid? IssueId { get; set; }
    public Issue? Issue { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public sealed class Session : MutableOwnedRecord
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string ActorUserId { get; set; } = string.Empty;
    public Guid? ActorAiClientId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Summary { get; set; }
}

public sealed class Activity : OwnedRecord
{
    public DateTimeOffset OccurredAt { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public Guid? ActorAiClientId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? SessionId { get; set; }
    public Session? Session { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public sealed class SavedView : MutableOwnedRecord
{
    public string Name { get; set; } = string.Empty;
    public SavedViewEntityType EntityType { get; set; }
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string ColumnsJson { get; set; } = "[]";
    public string FiltersJson { get; set; } = "{}";
    public string SortJson { get; set; } = "[]";
    public string? GroupBy { get; set; }
    public bool IsSystem { get; set; }
}

public sealed class AiClient : MutableOwnedRecord
{
    public string Name { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public DateTimeOffset? LastUsedAt { get; set; }
}
