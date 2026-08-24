using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record DocumentDto(
    Guid Id,
    string Title,
    string? Slug,
    string Body,
    Guid RevisionId,
    Guid? FolderId,
    Guid? ParentDocumentId,
    IReadOnlyList<Guid> ProjectIds,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record RevisionDto(
    Guid Id,
    string Title,
    string Body,
    string Kind,
    string ActorUserId,
    Guid? ActorAiClientId,
    DateTimeOffset CreatedAt);

public sealed record FolderDto(Guid Id, Guid? ParentFolderId, string Name, int Rank, DateTimeOffset? ArchivedAt);

public sealed record TemplateDto(Guid Id, string Name, string TitlePattern, string Body, DateTimeOffset? ArchivedAt);

public sealed class DocumentService(EnterpriseDbContext db, ActivityService activity, TimeProvider time)
{
    public async Task<DocumentDto> CreateAsync(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string title,
        string? body,
        Guid? folderId,
        Guid? templateId,
        string? slug,
        IReadOnlyList<Guid>? projectIds,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken,
        Guid? parentDocumentId = null)
    {
        var now = time.GetUtcNow();
        var resolvedTitle = title.Trim();
        var resolvedBody = body ?? string.Empty;
        if (templateId is Guid templateKey)
        {
            var template = await db.Set<DocumentTemplate>()
                .FirstOrDefaultAsync(item => item.Id == templateKey && item.ArchivedAt == null, cancellationToken)
                ?? throw new KeyNotFoundException();
            if (string.IsNullOrWhiteSpace(resolvedTitle))
            {
                resolvedTitle = template.TitlePattern;
            }

            if (string.IsNullOrEmpty(resolvedBody))
            {
                resolvedBody = template.Body;
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedTitle))
        {
            throw new ArgumentException("Title is required.");
        }

        if (folderId is Guid folderKey)
        {
            _ = await RequireFolderAsync(folderKey, cancellationToken);
        }

        if (parentDocumentId is Guid parentKey)
        {
            _ = await RequireParentDocumentAsync(parentKey, cancellationToken);
        }

        var documentId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            OwnerUserId = ownerUserId,
            Title = resolvedTitle,
            Slug = await UniqueDocumentSlugAsync(ownerUserId, slug, cancellationToken),
            FolderId = folderId,
            ParentDocumentId = parentDocumentId,
            IsProjectContext = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        var revision = new Revision
        {
            Id = revisionId,
            OwnerUserId = ownerUserId,
            DocumentId = documentId,
            Title = resolvedTitle,
            Body = resolvedBody,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            Kind = "save",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<Document>().Add(document);
        db.Set<Revision>().Add(revision);
        await db.SaveChangesAsync(cancellationToken);
        document.CurrentRevisionId = revisionId;
        if (projectIds is { Count: > 0 })
        {
            foreach (var projectId in projectIds.Distinct())
            {
                await LinkCoreAsync(document, projectId, cancellationToken);
            }
        }

        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags)
            {
                await AddTagCoreAsync(document, ownerUserId, tag, cancellationToken);
            }
        }

        AddActivity(ownerUserId, actorUserId, aiClientId, "created", documentId, "Created document");
        await db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(documentId.ToString("D"), cancellationToken))!;
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(bool includeArchived, CancellationToken cancellationToken)
    {
        var query = db.Set<Document>()
            .AsNoTracking()
            .Include(document => document.CurrentRevision)
            .Include(document => document.ProjectLinks)
            .Include(document => document.TagLinks)
            .ThenInclude(link => link.Tag)
            .Where(document => !document.IsProjectContext);
        if (!includeArchived)
        {
            query = query.Where(document => document.ArchivedAt == null);
        }

        var documents = await query.OrderBy(document => document.Title).ToListAsync(cancellationToken);
        return documents.Select(ToDto).ToList();
    }

    public async Task<DocumentDto?> GetAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var document = await FindAsync(idOrSlug, tracking: false, cancellationToken);
        return document is null || document.IsProjectContext ? null : ToDto(document);
    }

    public async Task<IReadOnlyList<RevisionDto>> ListRevisionsAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await db.Set<Document>().FirstOrDefaultAsync(item => item.Id == id && !item.IsProjectContext, cancellationToken);
        if (document is null)
        {
            return [];
        }

        return await db.Set<Revision>()
            .AsNoTracking()
            .Where(revision => revision.DocumentId == id)
            .OrderByDescending(revision => revision.CreatedAt)
            .Select(revision => new RevisionDto(
                revision.Id,
                revision.Title,
                revision.Body,
                revision.Kind,
                revision.ActorUserId,
                revision.ActorAiClientId,
                revision.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<DocumentDto> SaveAsync(
        string idOrSlug,
        string actorUserId,
        Guid? aiClientId,
        Guid expectedRevisionId,
        string? title,
        string body,
        CancellationToken cancellationToken) =>
        WriteAsync(idOrSlug, actorUserId, aiClientId, expectedRevisionId, title, body, "save", cancellationToken);

    public async Task<DocumentDto> AppendAsync(
        string idOrSlug,
        string actorUserId,
        Guid? aiClientId,
        Guid expectedRevisionId,
        string markdown,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(idOrSlug, tracking: true, cancellationToken)
            ?? throw new KeyNotFoundException();
        EnsureCurrent(document, expectedRevisionId);
        var nextBody = document.CurrentRevision!.Body + markdown;
        return await WriteAsync(
            idOrSlug, actorUserId, aiClientId, expectedRevisionId, document.Title, nextBody, "append", cancellationToken);
    }

    public async Task<DocumentDto> RestoreAsync(
        string idOrSlug,
        string actorUserId,
        Guid? aiClientId,
        Guid expectedRevisionId,
        Guid revisionId,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(idOrSlug, tracking: true, cancellationToken)
            ?? throw new KeyNotFoundException();
        EnsureCurrent(document, expectedRevisionId);
        var historical = await db.Set<Revision>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == revisionId && item.DocumentId == document.Id, cancellationToken)
            ?? throw new KeyNotFoundException();
        return await WriteAsync(
            idOrSlug,
            actorUserId,
            aiClientId,
            expectedRevisionId,
            historical.Title,
            historical.Body,
            "restore",
            cancellationToken);
    }

    public async Task<DocumentDto?> ArchiveAsync(
        Guid id,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(id.ToString("D"), tracking: true, cancellationToken);
        if (document is null)
        {
            return null;
        }

        if (document.ArchivedAt is null)
        {
            var now = time.GetUtcNow();
            document.ArchivedAt = now;
            document.UpdatedAt = now;
            AddActivity(document.OwnerUserId, actorUserId, aiClientId, "archived", document.Id, "Archived document");
            await db.SaveChangesAsync(cancellationToken);
        }

        return ToDto(document);
    }

    public async Task<DocumentDto> MoveAsync(
        Guid id,
        Guid? folderId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(id.ToString("D"), tracking: true, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (folderId is Guid folderKey)
        {
            _ = await RequireFolderAsync(folderKey, cancellationToken);
        }

        document.FolderId = folderId;
        document.UpdatedAt = time.GetUtcNow();
        AddActivity(document.OwnerUserId, actorUserId, aiClientId, "moved", document.Id, "Moved document");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(document);
    }

    public async Task<string?> LinkProjectAsync(
        Guid documentId,
        Guid projectId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(documentId.ToString("D"), tracking: true, cancellationToken);
        if (document is null)
        {
            return "not-found";
        }

        var error = await LinkCoreAsync(document, projectId, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        AddActivity(document.OwnerUserId, actorUserId, aiClientId, "linked", document.Id, "Linked project", projectId);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> UnlinkProjectAsync(
        Guid documentId,
        Guid projectId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(documentId.ToString("D"), tracking: true, cancellationToken);
        if (document is null)
        {
            return "not-found";
        }

        var link = await db.Set<DocumentProject>()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId && item.ProjectId == projectId, cancellationToken);
        if (link is not null)
        {
            db.Set<DocumentProject>().Remove(link);
            AddActivity(document.OwnerUserId, actorUserId, aiClientId, "unlinked", document.Id, "Unlinked project", projectId);
            await db.SaveChangesAsync(cancellationToken);
        }

        return null;
    }

    public async Task<DocumentDto> AddTagAsync(
        Guid documentId,
        string name,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(documentId.ToString("D"), tracking: true, cancellationToken)
            ?? throw new KeyNotFoundException();
        await AddTagCoreAsync(document, ownerUserId, name, cancellationToken);
        AddActivity(document.OwnerUserId, actorUserId, aiClientId, "tagged", document.Id, $"Added tag {name.Trim()}");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(document);
    }

    public async Task<IReadOnlyList<FolderDto>> ListFoldersAsync(CancellationToken cancellationToken) =>
        await db.Set<Folder>()
            .AsNoTracking()
            .Where(folder => folder.ArchivedAt == null)
            .OrderBy(folder => folder.Rank)
            .ThenBy(folder => folder.Name)
            .Select(folder => new FolderDto(folder.Id, folder.ParentFolderId, folder.Name, folder.Rank, folder.ArchivedAt))
            .ToListAsync(cancellationToken);

    public async Task<FolderDto> CreateFolderAsync(
        string ownerUserId,
        string name,
        Guid? parentFolderId,
        CancellationToken cancellationToken)
    {
        if (parentFolderId is Guid parentId)
        {
            _ = await RequireFolderAsync(parentId, cancellationToken);
        }

        var trimmed = name.Trim();
        var rank = await db.Set<Folder>()
            .Where(folder => folder.ParentFolderId == parentFolderId)
            .Select(folder => (int?)folder.Rank)
            .MaxAsync(cancellationToken) ?? -1;
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            ParentFolderId = parentFolderId,
            Name = trimmed,
            Rank = rank + 1,
            CreatedAt = time.GetUtcNow(),
            UpdatedAt = time.GetUtcNow()
        };
        db.Set<Folder>().Add(folder);
        await db.SaveChangesAsync(cancellationToken);
        return new FolderDto(folder.Id, folder.ParentFolderId, folder.Name, folder.Rank, folder.ArchivedAt);
    }

    public async Task<IReadOnlyList<TemplateDto>> ListTemplatesAsync(CancellationToken cancellationToken) =>
        await db.Set<DocumentTemplate>()
            .AsNoTracking()
            .Where(template => template.ArchivedAt == null)
            .OrderBy(template => template.Name)
            .Select(template => new TemplateDto(template.Id, template.Name, template.TitlePattern, template.Body, template.ArchivedAt))
            .ToListAsync(cancellationToken);

    public async Task<TemplateDto> CreateTemplateAsync(
        string ownerUserId,
        string name,
        string titlePattern,
        string body,
        CancellationToken cancellationToken)
    {
        var template = new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = name.Trim(),
            TitlePattern = titlePattern.Trim(),
            Body = body,
            CreatedAt = time.GetUtcNow(),
            UpdatedAt = time.GetUtcNow()
        };
        db.Set<DocumentTemplate>().Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return new TemplateDto(template.Id, template.Name, template.TitlePattern, template.Body, template.ArchivedAt);
    }

    private async Task<DocumentDto> WriteAsync(
        string idOrSlug,
        string actorUserId,
        Guid? aiClientId,
        Guid expectedRevisionId,
        string? title,
        string body,
        string kind,
        CancellationToken cancellationToken)
    {
        var document = await FindAsync(idOrSlug, tracking: true, cancellationToken)
            ?? throw new KeyNotFoundException();
        EnsureCurrent(document, expectedRevisionId);
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
            Kind = kind,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<Revision>().Add(revision);
        document.Title = nextTitle;
        document.CurrentRevisionId = revision.Id;
        document.CurrentRevision = revision;
        document.UpdatedAt = now;
        AddActivity(document.OwnerUserId, actorUserId, aiClientId, kind, document.Id, $"{kind} document");
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(document);
    }

    private async Task<Document?> FindAsync(string idOrSlug, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Document> query = db.Set<Document>()
            .Include(document => document.CurrentRevision)
            .Include(document => document.ProjectLinks)
            .Include(document => document.TagLinks)
            .ThenInclude(link => link.Tag);
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var document = Guid.TryParse(idOrSlug, out var id)
            ? await query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            : await query.FirstOrDefaultAsync(item => item.Slug == idOrSlug, cancellationToken);
        return document is { IsProjectContext: false } ? document : null;
    }

    private static void EnsureCurrent(Document document, Guid expectedRevisionId)
    {
        if (document.CurrentRevisionId != expectedRevisionId)
        {
            throw new ConflictException(
                "Document was updated by another writer.",
                new
                {
                    currentRevisionId = document.CurrentRevisionId,
                    actor = document.CurrentRevision?.ActorUserId,
                    updatedAt = document.CurrentRevision?.CreatedAt
                });
        }
    }

    private async Task<Folder> RequireFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
        await db.Set<Folder>().FirstOrDefaultAsync(item => item.Id == folderId && item.ArchivedAt == null, cancellationToken)
        ?? throw new KeyNotFoundException();

    private async Task<Document> RequireParentDocumentAsync(Guid parentDocumentId, CancellationToken cancellationToken) =>
        await db.Set<Document>().FirstOrDefaultAsync(
            item => item.Id == parentDocumentId && !item.IsProjectContext && item.ArchivedAt == null,
            cancellationToken)
        ?? throw new KeyNotFoundException();

    private async Task<string?> LinkCoreAsync(Document document, Guid projectId, CancellationToken cancellationToken)
    {
        if (document.IsProjectContext)
        {
            return "context-not-linkable";
        }

        var project = await db.Set<Project>().FirstOrDefaultAsync(item => item.Id == projectId, cancellationToken);
        if (project is null)
        {
            return "not-found";
        }

        var exists = await db.Set<DocumentProject>().AnyAsync(
            link => link.DocumentId == document.Id && link.ProjectId == projectId,
            cancellationToken);
        if (!exists)
        {
            db.Set<DocumentProject>().Add(new DocumentProject { DocumentId = document.Id, ProjectId = projectId });
        }

        return null;
    }

    private async Task AddTagCoreAsync(Document document, string ownerUserId, string name, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var tag = await db.Set<Tag>().FirstOrDefaultAsync(item => item.Name == trimmed, cancellationToken);
        if (tag is null)
        {
            tag = new Tag
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                Name = trimmed,
                CreatedAt = time.GetUtcNow(),
                UpdatedAt = time.GetUtcNow()
            };
            db.Set<Tag>().Add(tag);
        }

        var exists = await db.Set<DocumentTag>().AnyAsync(
            link => link.DocumentId == document.Id && link.TagId == tag.Id,
            cancellationToken);
        if (!exists)
        {
            db.Set<DocumentTag>().Add(new DocumentTag { DocumentId = document.Id, TagId = tag.Id });
        }
    }

    private async Task<string?> UniqueDocumentSlugAsync(
        string ownerUserId,
        string? slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var basis = ProjectService.Slugify(slug);
        var candidate = basis;
        var suffix = 2;
        while (await db.Set<Document>()
            .IgnoreQueryFilters()
            .AnyAsync(document => document.OwnerUserId == ownerUserId && document.Slug == candidate, cancellationToken))
        {
            candidate = $"{basis}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private void AddActivity(
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        string actionType,
        Guid recordId,
        string summary,
        Guid? projectId = null)
    {
        activity.Add(ownerUserId, actorUserId, aiClientId, actionType, "Document", recordId, projectId, summary);
    }

    private static DocumentDto ToDto(Document document) =>
        new(
            document.Id,
            document.CurrentRevision?.Title ?? document.Title,
            document.Slug,
            document.CurrentRevision?.Body ?? string.Empty,
            document.CurrentRevisionId ?? Guid.Empty,
            document.FolderId,
            document.ParentDocumentId,
            document.ProjectLinks.Select(link => link.ProjectId).ToArray(),
            document.TagLinks.Select(link => link.Tag.Name).ToArray(),
            document.UpdatedAt,
            document.ArchivedAt);
}
