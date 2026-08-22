using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record SearchHitDto(
    Guid Id,
    string Title,
    Guid? ProjectId,
    DateTimeOffset UpdatedAt,
    DateTimeOffset CreatedAt);

public sealed record SearchResultsDto(
    string Query,
    IReadOnlyList<SearchHitDto> Projects,
    IReadOnlyList<SearchHitDto> Documents,
    IReadOnlyList<SearchHitDto> Issues,
    IReadOnlyList<SearchHitDto> Activity);

public sealed class SearchService(EnterpriseDbContext db)
{
    private const int Limit = 25;

    public async Task<SearchResultsDto> SearchAsync(
        string query,
        string? type,
        Guid? projectId,
        string? tag,
        bool archived,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResultsDto(query ?? string.Empty, [], [], [], []);
        }

        var kind = type?.Trim().ToLowerInvariant();
        tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
        var projects = ShouldSearch(kind, "project")
            ? await SearchProjectsAsync(query, projectId, tag, archived, cancellationToken)
            : [];
        var documents = ShouldSearch(kind, "document")
            ? await SearchDocumentsAsync(query, projectId, tag, archived, cancellationToken)
            : [];
        var issues = ShouldSearch(kind, "issue")
            ? await SearchIssuesAsync(query, projectId, tag, archived, cancellationToken)
            : [];
        var activity = ShouldSearch(kind, "activity")
            ? await SearchActivityAsync(query, projectId, tag, cancellationToken)
            : [];
        return new SearchResultsDto(query, projects, documents, issues, activity);
    }

    private async Task<IReadOnlyList<SearchHitDto>> SearchProjectsAsync(
        string query,
        Guid? projectId,
        string? tag,
        bool archived,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tag))
        {
            return [];
        }

        return await db.Set<Project>()
            .Where(project =>
                project.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", query))
                && (archived || project.ArchivedAt == null)
                && (projectId == null || project.Id == projectId))
            .OrderByDescending(project =>
                EF.Functions.ToTsVector("simple", project.Name).Matches(EF.Functions.PlainToTsQuery("simple", query)))
            .ThenByDescending(project => project.UpdatedAt)
            .ThenByDescending(project => project.CreatedAt)
            .Take(Limit)
            .Select(project => new SearchHitDto(project.Id, project.Name, project.Id, project.UpdatedAt, project.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHitDto>> SearchDocumentsAsync(
        string query,
        Guid? projectId,
        string? tag,
        bool archived,
        CancellationToken cancellationToken)
    {
        return await db.Set<Document>()
            .Where(document =>
                document.CurrentRevision != null
                && document.CurrentRevision.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", query))
                && (archived || document.ArchivedAt == null)
                && (projectId == null || document.ProjectLinks.Any(link => link.ProjectId == projectId))
                && (tag == null || document.TagLinks.Any(link => link.Tag.Name == tag)))
            .OrderByDescending(document =>
                EF.Functions.ToTsVector("simple", document.Title).Matches(EF.Functions.PlainToTsQuery("simple", query)))
            .ThenByDescending(document => document.UpdatedAt)
            .ThenByDescending(document => document.CreatedAt)
            .Take(Limit)
            .Select(document => new SearchHitDto(
                document.Id,
                document.Title,
                document.ProjectLinks.Select(link => (Guid?)link.ProjectId).FirstOrDefault(),
                document.UpdatedAt,
                document.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHitDto>> SearchIssuesAsync(
        string query,
        Guid? projectId,
        string? tag,
        bool archived,
        CancellationToken cancellationToken)
    {
        return await db.Set<Issue>()
            .Where(issue =>
                issue.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", query))
                && (archived || issue.ArchivedAt == null)
                && (projectId == null || issue.ProjectId == projectId)
                && (tag == null || issue.TagLinks.Any(link => link.Tag.Name == tag)))
            .OrderByDescending(issue =>
                EF.Functions.ToTsVector("simple", issue.Title).Matches(EF.Functions.PlainToTsQuery("simple", query)))
            .ThenByDescending(issue => issue.UpdatedAt)
            .ThenByDescending(issue => issue.CreatedAt)
            .Take(Limit)
            .Select(issue => new SearchHitDto(issue.Id, issue.Title, issue.ProjectId, issue.UpdatedAt, issue.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SearchHitDto>> SearchActivityAsync(
        string query,
        Guid? projectId,
        string? tag,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tag))
        {
            return [];
        }

        return await db.Set<Activity>()
            .Where(item =>
                item.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", query))
                && (projectId == null || item.ProjectId == projectId))
            .OrderByDescending(item => item.OccurredAt)
            .Take(Limit)
            .Select(item => new SearchHitDto(
                item.Id,
                item.Summary,
                item.ProjectId,
                item.UpdatedAt,
                item.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static bool ShouldSearch(string? kind, string entity) =>
        string.IsNullOrWhiteSpace(kind) || kind == entity;
}
