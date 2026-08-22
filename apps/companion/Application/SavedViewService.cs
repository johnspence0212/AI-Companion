using System.Text.Json;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record SavedViewSortDto(string Field, string Direction);

public sealed record SavedViewDto(
    Guid Id,
    string Name,
    string EntityType,
    Guid? ProjectId,
    IReadOnlyList<string> Columns,
    IReadOnlyDictionary<string, string> Filters,
    IReadOnlyList<SavedViewSortDto> Sort,
    string? GroupBy,
    bool IsSystem);

public static class SystemSavedViews
{
    public static readonly Guid DocumentsAll = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid DocumentsUnfiled = Guid.Parse("11111111-1111-4111-8111-111111111112");
    public static readonly Guid IssuesByStatus = Guid.Parse("22222222-2222-4222-8222-222222222221");
    public static readonly Guid IssuesActive = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid ActivityRecent = Guid.Parse("44444444-4444-4444-8444-444444444441");
    public static readonly Guid ActivityIssues = Guid.Parse("44444444-4444-4444-8444-444444444442");

    public static IReadOnlyList<SavedViewDto> For(SavedViewEntityType entityType, Guid? projectId) =>
        Catalog.Where(view =>
                view.EntityType == entityType.ToString()
                && (projectId is null || view.ProjectId is null || view.ProjectId == projectId))
            .ToList();

    public static SavedViewDto? Find(Guid id) =>
        Catalog.FirstOrDefault(view => view.Id == id);

    private static readonly SavedViewDto[] Catalog =
    [
        new(
            DocumentsAll,
            "All Documents",
            nameof(SavedViewEntityType.Documents),
            null,
            ["title", "updatedAt"],
            new Dictionary<string, string>(),
            [new SavedViewSortDto("updatedAt", "desc")],
            null,
            true),
        new(
            DocumentsUnfiled,
            "Unfiled",
            nameof(SavedViewEntityType.Documents),
            null,
            ["title", "updatedAt"],
            new Dictionary<string, string> { ["folder"] = "unfiled" },
            [new SavedViewSortDto("updatedAt", "desc")],
            null,
            true),
        new(
            IssuesByStatus,
            "Issues by status",
            nameof(SavedViewEntityType.Issues),
            null,
            ["title", "status", "priority"],
            new Dictionary<string, string>(),
            [new SavedViewSortDto("rank", "asc")],
            "status",
            true),
        new(
            IssuesActive,
            "Active",
            nameof(SavedViewEntityType.Issues),
            null,
            ["title", "status", "priority"],
            new Dictionary<string, string> { ["status"] = "Active" },
            [new SavedViewSortDto("rank", "asc")],
            null,
            true),
        new(
            ActivityRecent,
            "Recent Activity",
            nameof(SavedViewEntityType.Activity),
            null,
            ["summary", "occurredAt"],
            new Dictionary<string, string>(),
            [new SavedViewSortDto("occurredAt", "desc")],
            null,
            true),
        new(
            ActivityIssues,
            "Issue Activity",
            nameof(SavedViewEntityType.Activity),
            null,
            ["summary", "occurredAt"],
            new Dictionary<string, string> { ["recordType"] = "Issue" },
            [new SavedViewSortDto("occurredAt", "desc")],
            null,
            true),
    ];
}

public sealed class SavedViewService(EnterpriseDbContext db, TimeProvider time)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyList<SavedViewDto>> ListAsync(
        SavedViewEntityType? entityType,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var stored = await db.Set<SavedView>()
            .Where(view =>
                (entityType == null || view.EntityType == entityType)
                && (projectId == null || view.ProjectId == null || view.ProjectId == projectId))
            .OrderBy(view => view.Name)
            .ToListAsync(cancellationToken);

        var system = entityType is SavedViewEntityType type
            ? SystemSavedViews.For(type, projectId)
            : SystemSavedViews.For(SavedViewEntityType.Documents, projectId)
                .Concat(SystemSavedViews.For(SavedViewEntityType.Issues, projectId))
                .Concat(SystemSavedViews.For(SavedViewEntityType.Activity, projectId));

        return system.Concat(stored.Select(ToDto)).ToList();
    }

    public async Task<SavedViewDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var system = SystemSavedViews.Find(id);
        if (system is not null)
        {
            return system;
        }

        var stored = await db.Set<SavedView>().FirstOrDefaultAsync(view => view.Id == id, cancellationToken);
        return stored is null ? null : ToDto(stored);
    }

    public async Task<SavedViewDto> DuplicateAsync(
        Guid sourceId,
        string ownerUserId,
        string? name,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var source = await GetAsync(sourceId, cancellationToken) ?? throw new KeyNotFoundException();
        if (!Enum.TryParse<SavedViewEntityType>(source.EntityType, out var entityType))
        {
            throw new ProtocolException("Saved View entity type is invalid.");
        }

        if (projectId is Guid project && !await db.Set<Project>().AnyAsync(item => item.Id == project, cancellationToken))
        {
            throw new KeyNotFoundException();
        }

        var now = time.GetUtcNow();
        var copy = new SavedView
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Name = await UniqueNameAsync(ownerUserId, name, source.Name, projectId ?? source.ProjectId, cancellationToken),
            EntityType = entityType,
            ProjectId = projectId ?? source.ProjectId,
            ColumnsJson = JsonSerializer.Serialize(source.Columns, Json),
            FiltersJson = JsonSerializer.Serialize(source.Filters, Json),
            SortJson = JsonSerializer.Serialize(source.Sort, Json),
            GroupBy = source.GroupBy,
            IsSystem = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<SavedView>().Add(copy);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(copy);
    }

    public async Task<SavedViewDto> UpdateAsync(
        Guid id,
        string name,
        IReadOnlyList<string>? columns,
        IReadOnlyDictionary<string, string>? filters,
        IReadOnlyList<SavedViewSortDto>? sort,
        string? groupBy,
        CancellationToken cancellationToken)
    {
        if (SystemSavedViews.Find(id) is not null)
        {
            throw new ProtocolException("System Saved Views are read-only. Duplicate the view to change filters.");
        }

        var view = await db.Set<SavedView>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (view.IsSystem)
        {
            throw new ProtocolException("System Saved Views are read-only. Duplicate the view to change filters.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProtocolException("Name is required.");
        }

        view.Name = name.Trim();
        if (columns is not null)
        {
            view.ColumnsJson = JsonSerializer.Serialize(columns, Json);
        }

        if (filters is not null)
        {
            view.FiltersJson = JsonSerializer.Serialize(filters, Json);
        }

        if (sort is not null)
        {
            view.SortJson = JsonSerializer.Serialize(sort, Json);
        }

        view.GroupBy = string.IsNullOrWhiteSpace(groupBy) ? null : groupBy.Trim();
        view.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(view);
    }

    private async Task<string> UniqueNameAsync(
        string ownerUserId,
        string? requested,
        string sourceName,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var baseName = string.IsNullOrWhiteSpace(requested) ? $"{sourceName} (copy)" : requested.Trim();
        var candidate = baseName;
        var suffix = 2;
        while (await db.Set<SavedView>().AnyAsync(
                   view => view.OwnerUserId == ownerUserId
                       && view.Name == candidate
                       && view.ProjectId == projectId,
                   cancellationToken))
        {
            candidate = $"{baseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    private static SavedViewDto ToDto(SavedView view) =>
        new(
            view.Id,
            view.Name,
            view.EntityType.ToString(),
            view.ProjectId,
            Deserialize(view.ColumnsJson, Array.Empty<string>()),
            Deserialize(view.FiltersJson, new Dictionary<string, string>()),
            Deserialize(view.SortJson, Array.Empty<SavedViewSortDto>()),
            view.GroupBy,
            view.IsSystem);

    private static T Deserialize<T>(string json, T fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Json) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
