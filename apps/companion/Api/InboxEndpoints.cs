using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class InboxEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var inbox = endpoints.MapGroup("/api/v1/inbox");
        inbox.MapGet(
                "/",
                async (InboxStatus? status, InboxService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.ListAsync(status, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.InboxRead);
        inbox.MapGet(
                "/{id:guid}",
                async (Guid id, InboxService service, CancellationToken cancellationToken) =>
                {
                    var item = await service.GetAsync(id, cancellationToken);
                    return item is null ? Results.NotFound() : Results.Ok(item);
                })
            .RequireAuthorization(CompanionPermissions.InboxRead);
        inbox.MapPost(
                "/",
                async (CaptureInboxRequest request, ClaimsPrincipal user, InboxService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                    {
                        var created = await service.CaptureAsync(userId, userId, clientId, request.Text, cancellationToken);
                        return (created, Created: true);
                    }))
            .RequireAuthorization(CompanionPermissions.InboxManage);
        inbox.MapPost(
                "/{id:guid}/process",
                async (Guid id, ProcessInboxRequest request, ClaimsPrincipal user, InboxService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                    {
                        var processed = await service.ProcessAsync(
                            id,
                            userId,
                            userId,
                            clientId,
                            request.Title,
                            request.ProjectId,
                            request.CreateDocument,
                            request.CreateIssue,
                            request.DocumentId,
                            request.IssueId,
                            cancellationToken);
                        return (processed, Created: false);
                    }))
            .RequireAuthorization(CompanionPermissions.InboxManage);
        inbox.MapPost(
                "/{id:guid}/archive",
                async (Guid id, ClaimsPrincipal user, InboxService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                        (await service.ArchiveAsync(id, userId, clientId, cancellationToken), Created: false)))
            .RequireAuthorization(CompanionPermissions.InboxManage);
        inbox.MapPost(
                "/{id:guid}/reopen",
                async (Guid id, ClaimsPrincipal user, InboxService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                        (await service.ReopenAsync(id, userId, clientId, cancellationToken), Created: false)))
            .RequireAuthorization(CompanionPermissions.InboxManage);
    }

    private static async Task<IResult> Write(
        ClaimsPrincipal user,
        Func<string, Guid?, Task<(InboxItemDto Item, bool Created)>> action)
    {
        try
        {
            var (userId, clientId) = ProjectService.ActorFrom(user);
            var (item, created) = await action(userId, clientId);
            return created
                ? Results.Created($"/api/v1/inbox/{item.Id}", item)
                : Results.Ok(item);
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

public sealed record CaptureInboxRequest([Required, StringLength(4000)] string Text);

public sealed record ProcessInboxRequest(
    [StringLength(500)] string? Title,
    Guid? ProjectId,
    bool CreateDocument,
    bool CreateIssue,
    Guid? DocumentId,
    Guid? IssueId);
