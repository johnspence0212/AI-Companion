using System.Security.Claims;
using System.Text.RegularExpressions;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.ModuleAbstractions;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string Slug,
    Guid ContextDocumentId,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record ProjectContextDto(
    Guid ProjectId,
    string ProjectSlug,
    Guid DocumentId,
    Guid RevisionId,
    string Title,
    string Body,
    DateTimeOffset UpdatedAt);

public sealed class ConflictException(string message, object? details = null) : Exception(message)
{
    public object? Details { get; } = details;
}

public sealed class ProjectService(EnterpriseDbContext db, TimeProvider time) : IAfterSignInHandler
{
    public const string BootstrapName = "Bootstrap";
    public const string BootstrapSlug = "bootstrap";

    public static string ContextSkeleton(string projectName)
    {
        var heading = string.IsNullOrWhiteSpace(projectName) ? "Project" : projectName.Trim();
        return $"""
            # {heading}

            ## Goal

            ## Current State

            ## Architecture

            ## Key Decisions

            ## Constraints

            ## Open Questions

            ## Current Priorities

            ## Important Links

            ## Notes for AI Agents

            """;
    }

    public Task HandleAsync(string userId, CancellationToken cancellationToken) =>
        EnsureBootstrapAsync(userId, userId, aiClientId: null, cancellationToken);

    public async Task<ProjectDto> EnsureBootstrapAsync(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var existing = await db.Set<Project>()
            .IgnoreQueryFilters()
            .Where(project => project.OwnerUserId == ownerUserId)
            .OrderBy(project => project.CreatedAt)
            .Select(project => ToDto(project))
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        return await CreateCoreAsync(ownerUserId, actorUserId, aiClientId, BootstrapName, BootstrapSlug, cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string name,
        CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var slug = await UniqueSlugAsync(ownerUserId, Slugify(trimmed), cancellationToken);
        return await CreateCoreAsync(ownerUserId, actorUserId, aiClientId, trimmed, slug, cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.Set<Project>()
            .AsNoTracking()
            .Where(project => project.ArchivedAt == null)
            .OrderBy(project => project.Name)
            .Select(project => ToDto(project))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto?> GetAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var query = db.Set<Project>().AsNoTracking();
        var project = Guid.TryParse(idOrSlug, out var id)
            ? await query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            : await query.FirstOrDefaultAsync(item => item.Slug == idOrSlug, cancellationToken);
        return project is null ? null : ToDto(project);
    }

    public async Task<ProjectDto?> ArchiveAsync(
        Guid id,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var project = await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (project.ArchivedAt is null)
        {
            var now = time.GetUtcNow();
            project.ArchivedAt = now;
            project.UpdatedAt = now;
            project.Version++;
            AddActivity(actorUserId, aiClientId, "archived", "Project", project.Id, project.Id, "Archived project");
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(project);
    }

    public async Task<ProjectContextDto?> GetContextAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var project = await FindTrackedAsync(idOrSlug, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var document = await db.Set<Document>()
            .Include(item => item.CurrentRevision)
            .FirstOrDefaultAsync(item => item.Id == project.ContextDocumentId, cancellationToken);
        if (document?.CurrentRevision is null)
        {
            return null;
        }

        return ToContextDto(project, document, document.CurrentRevision);
    }

    public async Task<ProjectContextDto?> GetCurrentContextAsync(CancellationToken cancellationToken)
    {
        var current = await db.Set<Project>()
            .AsNoTracking()
            .Where(project => project.ArchivedAt == null)
            .OrderBy(project => project.CreatedAt)
            .Select(project => project.Slug)
            .FirstOrDefaultAsync(cancellationToken);
        return current is null ? null : await GetContextAsync(current, cancellationToken);
    }

    public async Task<ProjectContextDto> UpdateContextAsync(
        string idOrSlug,
        string actorUserId,
        Guid? aiClientId,
        Guid expectedRevisionId,
        string? title,
        string body,
        CancellationToken cancellationToken)
    {
        var project = await FindTrackedAsync(idOrSlug, cancellationToken)
            ?? throw new KeyNotFoundException();
        var document = await db.Set<Document>()
            .Include(item => item.CurrentRevision)
            .FirstOrDefaultAsync(item => item.Id == project.ContextDocumentId, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (document.CurrentRevisionId != expectedRevisionId)
        {
            throw new ConflictException(
                "Context was updated by another writer.",
                new
                {
                    currentRevisionId = document.CurrentRevisionId,
                    actor = document.CurrentRevision?.ActorUserId,
                    updatedAt = document.CurrentRevision?.CreatedAt
                });
        }

        var now = time.GetUtcNow();
        var nextTitle = string.IsNullOrWhiteSpace(title) ? document.Title : title.Trim();
        var revision = new Revision
        {
            Id = Guid.NewGuid(),
            OwnerUserId = document.OwnerUserId,
            DocumentId = document.Id,
            Title = nextTitle,
            Body = body,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            Kind = "save",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<Revision>().Add(revision);
        document.Title = nextTitle;
        document.CurrentRevisionId = revision.Id;
        document.UpdatedAt = now;
        project.UpdatedAt = now;
        AddActivity(
            actorUserId,
            aiClientId,
            "updated",
            "Document",
            document.Id,
            project.Id,
            "Updated Project Context",
            sessionId: null);
        await db.SaveChangesAsync(cancellationToken);
        return ToContextDto(project, document, revision);
    }

    public async Task<string?> TryLinkDocumentAsync(
        Guid projectId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var project = await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        var document = await db.Set<Document>().FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        if (project is null || document is null)
        {
            return "not-found";
        }

        if (document.IsProjectContext)
        {
            return "context-not-linkable";
        }

        var exists = await db.Set<DocumentProject>().AnyAsync(
            link => link.ProjectId == projectId && link.DocumentId == documentId,
            cancellationToken);
        if (!exists)
        {
            db.Set<DocumentProject>().Add(new DocumentProject { ProjectId = projectId, DocumentId = documentId });
            await db.SaveChangesAsync(cancellationToken);
        }

        return null;
    }

    public Task<List<Document>> ListLibraryAsync(CancellationToken cancellationToken) =>
        db.Set<Document>()
            .AsNoTracking()
            .Where(document => !document.IsProjectContext && document.ArchivedAt == null)
            .OrderBy(document => document.Title)
            .ToListAsync(cancellationToken);

    private async Task<ProjectDto> CreateCoreAsync(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string name,
        string slug,
        CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var documentId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var title = $"{name} Context";
        var body = ContextSkeleton(name);
        var document = new Document
        {
            Id = documentId,
            OwnerUserId = ownerUserId,
            Title = title,
            Slug = null,
            FolderId = null,
            IsProjectContext = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var revision = new Revision
        {
            Id = revisionId,
            OwnerUserId = ownerUserId,
            DocumentId = documentId,
            Title = title,
            Body = body,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            Kind = "save",
            CreatedAt = now,
            UpdatedAt = now
        };
        var project = new Project
        {
            Id = projectId,
            OwnerUserId = ownerUserId,
            Name = name,
            Slug = slug,
            ContextDocumentId = documentId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<Document>().Add(document);
        db.Set<Revision>().Add(revision);
        db.Set<Project>().Add(project);
        await db.SaveChangesAsync(cancellationToken);
        document.CurrentRevisionId = revisionId;
        AddActivity(actorUserId, aiClientId, "created", "Project", projectId, projectId, $"Created project {name}");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(project);
    }

    private async Task<Project?> FindTrackedAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        return Guid.TryParse(idOrSlug, out var id)
            ? await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            : await db.Set<Project>().FirstOrDefaultAsync(item => item.Slug == idOrSlug, cancellationToken);
    }

    private async Task<string> UniqueSlugAsync(string ownerUserId, string basis, CancellationToken cancellationToken)
    {
        var slug = basis;
        var suffix = 2;
        while (await db.Set<Project>()
            .IgnoreQueryFilters()
            .AnyAsync(project => project.OwnerUserId == ownerUserId && project.Slug == slug, cancellationToken))
        {
            var trimmed = basis.Length > 120 ? basis[..120] : basis;
            slug = $"{trimmed}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private void AddActivity(
        string actorUserId,
        Guid? aiClientId,
        string actionType,
        string recordType,
        Guid recordId,
        Guid? projectId,
        string summary,
        Guid? sessionId = null)
    {
        var now = time.GetUtcNow();
        db.Set<Activity>().Add(new Activity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = actorUserId,
            OccurredAt = now,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            ActionType = actionType,
            RecordType = recordType,
            RecordId = recordId,
            ProjectId = projectId,
            SessionId = sessionId,
            Summary = summary,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static ProjectDto ToDto(Project project) =>
        new(project.Id, project.Name, project.Slug, project.ContextDocumentId, project.Version, project.CreatedAt, project.ArchivedAt);

    private static ProjectContextDto ToContextDto(Project project, Document document, Revision revision) =>
        new(project.Id, project.Slug, document.Id, revision.Id, revision.Title, revision.Body, document.UpdatedAt);

    internal static string Slugify(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
        {
            slug = "project";
        }

        return slug.Length <= 128 ? slug : slug[..128].TrimEnd('-');
    }

    public static (string UserId, Guid? AiClientId) ActorFrom(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var client = user.FindFirstValue(AiClientAuth.ClientIdClaim);
        return (userId, Guid.TryParse(client, out var id) ? id : null);
    }
}
