using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class SessionEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/v1/projects/{projectId:guid}/sessions",
                async (Guid projectId, ClaimsPrincipal user, SessionService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                    {
                        var started = await service.StartAsync(projectId, userId, userId, clientId, cancellationToken);
                        return (started, Created: true);
                    }))
            .RequireAuthorization(CompanionPermissions.SessionsManage);
        endpoints.MapGet(
                "/api/v1/projects/{projectId:guid}/sessions",
                async (Guid projectId, SessionService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        return Results.Ok(await service.ListAsync(projectId, cancellationToken));
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                })
            .RequireAuthorization(CompanionPermissions.SessionsRead);
        endpoints.MapGet(
                "/api/v1/sessions/{id:guid}",
                async (Guid id, SessionService service, CancellationToken cancellationToken) =>
                {
                    var session = await service.GetAsync(id, cancellationToken);
                    return session is null ? Results.NotFound() : Results.Ok(session);
                })
            .RequireAuthorization(CompanionPermissions.SessionsRead);
        endpoints.MapPost(
                "/api/v1/sessions/{id:guid}/finish",
                async (Guid id, FinishSessionRequest request, ClaimsPrincipal user, SessionService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                        (await service.FinishAsync(id, request.Summary, userId, clientId, cancellationToken), Created: false)))
            .RequireAuthorization(CompanionPermissions.SessionsManage);

        endpoints.MapGet(
                "/api/v1/activity",
                async (Guid? projectId, string? recordType, Guid? sessionId, ActivityService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.ListAsync(projectId, recordType, sessionId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.ActivityRead);
    }

    private static async Task<IResult> Write(
        ClaimsPrincipal user,
        Func<string, Guid?, Task<(SessionDto Session, bool Created)>> action)
    {
        try
        {
            var (userId, clientId) = ProjectService.ActorFrom(user);
            var (session, created) = await action(userId, clientId);
            return created
                ? Results.Created($"/api/v1/sessions/{session.Id}", session)
                : Results.Ok(session);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ProtocolException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}

public sealed record FinishSessionRequest([Required] string Summary);
