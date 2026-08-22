using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record ActivityDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorUserId,
    Guid? ActorAiClientId,
    string ActionType,
    string RecordType,
    Guid RecordId,
    Guid? ProjectId,
    Guid? SessionId,
    string Summary);

public sealed class ActivityService(EnterpriseDbContext db, TimeProvider time)
{
    public async Task<IReadOnlyList<ActivityDto>> ListAsync(
        Guid? projectId,
        string? recordType,
        Guid? sessionId,
        CancellationToken cancellationToken)
    {
        var query = db.Set<Activity>().AsQueryable();
        if (projectId is Guid project)
        {
            query = query.Where(item => item.ProjectId == project);
        }

        if (!string.IsNullOrWhiteSpace(recordType))
        {
            query = query.Where(item => item.RecordType == recordType);
        }

        if (sessionId is Guid session)
        {
            query = query.Where(item => item.SessionId == session);
        }

        return await query
            .OrderByDescending(item => item.OccurredAt)
            .Take(200)
            .Select(item => new ActivityDto(
                item.Id,
                item.OccurredAt,
                item.ActorUserId,
                item.ActorAiClientId,
                item.ActionType,
                item.RecordType,
                item.RecordId,
                item.ProjectId,
                item.SessionId,
                item.Summary))
            .ToListAsync(cancellationToken);
    }

    public void Add(
        string ownerUserId,
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
            OwnerUserId = ownerUserId,
            OccurredAt = now,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            ActionType = actionType,
            RecordType = recordType,
            RecordId = recordId,
            ProjectId = projectId,
            SessionId = sessionId ?? FindOpenSessionId(projectId, actorUserId, aiClientId),
            Summary = TrimSummary(summary),
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Guid? FindOpenSessionId(Guid? projectId, string actorUserId, Guid? aiClientId)
    {
        if (projectId is not Guid project)
        {
            return null;
        }

        return db.Set<Session>()
            .Local
            .Where(session => IsOpenFor(session, project, actorUserId, aiClientId))
            .Select(session => (Guid?)session.Id)
            .FirstOrDefault()
            ?? db.Set<Session>()
                .Where(session =>
                    session.ProjectId == project
                    && session.FinishedAt == null
                    && session.ActorUserId == actorUserId
                    && session.ActorAiClientId == aiClientId)
                .Select(session => (Guid?)session.Id)
                .FirstOrDefault();
    }

    private static bool IsOpenFor(Session session, Guid projectId, string actorUserId, Guid? aiClientId) =>
        session.ProjectId == projectId
        && session.FinishedAt == null
        && session.ActorUserId == actorUserId
        && session.ActorAiClientId == aiClientId;

    private static string TrimSummary(string summary) =>
        summary.Length <= 500 ? summary : summary[..500];
}
