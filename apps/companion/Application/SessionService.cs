using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseStarter.Companion.Application;

public sealed record SessionTouchedDto(string RecordType, Guid RecordId, string Summary);

public sealed record SessionDto(
    Guid Id,
    Guid ProjectId,
    string ActorUserId,
    Guid? ActorAiClientId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string? Summary,
    IReadOnlyList<SessionTouchedDto> Touched);

public sealed class SessionService(EnterpriseDbContext db, ActivityService activity, TimeProvider time)
{
    public async Task<SessionDto> StartAsync(
        Guid projectId,
        string ownerUserId,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        _ = await db.Set<Project>().FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (activity.FindOpenSessionId(projectId, actorUserId, aiClientId) is not null)
        {
            throw new ProtocolException("An open Session already exists for this actor and Project.");
        }

        var now = time.GetUtcNow();
        var session = new Session
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            ProjectId = projectId,
            ActorUserId = actorUserId,
            ActorAiClientId = aiClientId,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Set<Session>().Add(session);
        activity.Add(
            ownerUserId,
            actorUserId,
            aiClientId,
            "started",
            "Session",
            session.Id,
            projectId,
            "Started session",
            session.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(session, []);
    }

    public async Task<IReadOnlyList<SessionDto>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await db.Set<Project>().AnyAsync(project => project.Id == projectId, cancellationToken))
        {
            throw new KeyNotFoundException();
        }

        var sessions = await db.Set<Session>()
            .Where(session => session.ProjectId == projectId)
            .OrderByDescending(session => session.StartedAt)
            .ToListAsync(cancellationToken);
        return sessions.Select(session => ToDto(session, [])).ToList();
    }

    public async Task<SessionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await db.Set<Session>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return session is null ? null : ToDto(session, await TouchedAsync(id, cancellationToken));
    }

    public async Task<SessionDto> FinishAsync(
        Guid id,
        string summary,
        string actorUserId,
        Guid? aiClientId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ProtocolException("Finish requires a Markdown summary.");
        }

        var session = await db.Set<Session>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException();
        if (session.FinishedAt is not null)
        {
            throw new ProtocolException("This Session is already finished.");
        }

        if (session.ActorAiClientId != aiClientId
            || (aiClientId is null && session.ActorUserId != actorUserId))
        {
            throw new ProtocolException("Only the actor who started the Session can finish it.");
        }

        session.FinishedAt = time.GetUtcNow();
        session.Summary = summary;
        session.UpdatedAt = session.FinishedAt.Value;
        activity.Add(
            session.OwnerUserId,
            actorUserId,
            aiClientId,
            "finished",
            "Session",
            session.Id,
            session.ProjectId,
            summary,
            session.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(session, await TouchedAsync(session.Id, cancellationToken));
    }

    private async Task<IReadOnlyList<SessionTouchedDto>> TouchedAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await db.Set<Activity>()
            .Where(item =>
                item.SessionId == sessionId
                && (item.RecordType == "Issue" || item.RecordType == "Document"))
            .OrderBy(item => item.OccurredAt)
            .Select(item => new SessionTouchedDto(item.RecordType, item.RecordId, item.Summary))
            .ToListAsync(cancellationToken);
    }

    private static SessionDto ToDto(Session session, IReadOnlyList<SessionTouchedDto> touched) =>
        new(
            session.Id,
            session.ProjectId,
            session.ActorUserId,
            session.ActorAiClientId,
            session.StartedAt,
            session.FinishedAt,
            session.Summary,
            touched);
}
