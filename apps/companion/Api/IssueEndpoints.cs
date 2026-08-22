using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class IssueEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var projectIssues = endpoints.MapGroup("/api/v1/projects/{projectId:guid}/issues");
        projectIssues.MapGet(
                "/",
                async (Guid projectId, IssueService service, CancellationToken cancellationToken) =>
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
            .RequireAuthorization(CompanionPermissions.IssuesRead);
        projectIssues.MapPost(
                "/",
                async (Guid projectId, CreateIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Title))
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["title"] = ["Title is required."]
                        });
                    }

                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    return await Read(async () =>
                    {
                        var created = await service.CreateAsync(
                            projectId,
                            userId,
                            userId,
                            clientId,
                            request.Title,
                            request.Description,
                            request.Status ?? IssueStatus.Backlog,
                            request.Priority ?? IssuePriority.Normal,
                            request.ParentIssueId,
                            cancellationToken);
                        return created;
                    }, created: true);
                })
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        projectIssues.MapGet(
                "/next",
                async (Guid projectId, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    return await Write(() => service.GetNextAsync(projectId, userId, clientId, cancellationToken), allowEmpty: true);
                })
            .RequireAuthorization(CompanionPermissions.IssuesRead);
        projectIssues.MapPost(
                "/claim-next",
                async (Guid projectId, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    return await Write(() => service.ClaimNextAsync(projectId, userId, clientId, cancellationToken), allowEmpty: true);
                })
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        projectIssues.MapPost(
                "/start-next",
                async (Guid projectId, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    return await Write(() => service.StartNextAsync(projectId, userId, clientId, cancellationToken), allowEmpty: true);
                })
            .RequireAuthorization(CompanionPermissions.IssuesManage);

        var issues = endpoints.MapGroup("/api/v1/issues");
        issues.MapGet(
                "/{id:guid}",
                async (Guid id, IssueService service, CancellationToken cancellationToken) =>
                    await Read(() => service.GetAsync(id, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesRead);
        issues.MapPost(
                "/{id:guid}/claim",
                async (Guid id, VersionedIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.ClaimAsync(id, request.ExpectedVersion, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/start",
                async (Guid id, VersionedIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.StartAsync(id, request.ExpectedVersion, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/block",
                async (Guid id, ReasonIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.BlockAsync(id, request.ExpectedVersion, request.Reason, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/unblock",
                async (Guid id, VersionedIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.UnblockAsync(id, request.ExpectedVersion, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/complete",
                async (Guid id, ResolutionIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.CompleteAsync(id, request.ExpectedVersion, request.Resolution, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/cancel",
                async (Guid id, ReasonIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.CancelAsync(id, request.ExpectedVersion, request.Reason, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/reopen",
                async (Guid id, VersionedIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.ReopenAsync(id, request.ExpectedVersion, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/move",
                async (Guid id, MoveIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.MoveAsync(id, request.ExpectedVersion, request.Status, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/blockers",
                async (Guid id, AddBlockerRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                    await Mutate(user, service, (userId, clientId) =>
                        service.AddBlockerAsync(id, request.BlockerIssueId, request.ExpectedVersion, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.IssuesManage);
        issues.MapPost(
                "/{id:guid}/archive",
                async (Guid id, VersionedIssueRequest request, ClaimsPrincipal user, IssueService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        var archived = await service.ArchiveAsync(
                            id, request.ExpectedVersion, userId, clientId, cancellationToken);
                        return archived is null ? Results.NotFound() : Results.NoContent();
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                    catch (ProtocolException ex)
                    {
                        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                    }
                    catch (ConflictException ex)
                    {
                        return Conflict(ex);
                    }
                })
            .RequireAuthorization(CompanionPermissions.IssuesManage);
    }

    private static Task<IResult> Mutate(
        ClaimsPrincipal user,
        IssueService _,
        Func<string, Guid?, Task<IssueDto>> action)
    {
        var (userId, clientId) = ProjectService.ActorFrom(user);
        return Write(async () => await action(userId, clientId));
    }

    private static async Task<IResult> Read<T>(Func<Task<T?>> action, bool created = false)
    {
        try
        {
            var result = await action();
            if (result is null)
            {
                return Results.NotFound();
            }

            return created && result is IssueDto createdIssue
                ? Results.Created($"/api/v1/issues/{createdIssue.Id}", createdIssue)
                : Results.Ok(result);
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

    private static async Task<IResult> Write(Func<Task<IssueDto?>> action, bool allowEmpty = false)
    {
        try
        {
            var result = await action();
            if (result is null)
            {
                return allowEmpty ? Results.Ok(result) : Results.NotFound();
            }

            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ProtocolException ex)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
        catch (ConflictException ex)
        {
            return Conflict(ex);
        }
    }

    private static IResult Conflict(ConflictException ex) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: ex.Message,
            extensions: ex.Details is null ? null : new Dictionary<string, object?> { ["current"] = ex.Details });
}

public sealed record CreateIssueRequest(
    [Required, StringLength(500)] string Title,
    string? Description,
    IssueStatus? Status,
    IssuePriority? Priority,
    Guid? ParentIssueId);

public sealed record VersionedIssueRequest([Required] int ExpectedVersion);

public sealed record ReasonIssueRequest([Required] int ExpectedVersion, [Required] string Reason);

public sealed record ResolutionIssueRequest([Required] int ExpectedVersion, [Required] string Resolution);

public sealed record MoveIssueRequest([Required] int ExpectedVersion, [Required] IssueStatus Status);

public sealed record AddBlockerRequest([Required] Guid BlockerIssueId, [Required] int ExpectedVersion);
