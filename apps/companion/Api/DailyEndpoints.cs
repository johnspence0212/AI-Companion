using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class DailyEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var daily = endpoints.MapGroup("/api/v1/daily");
        daily.MapGet(
                "/",
                async (DateOnly? date, DailyService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetAsync(date, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.DailyRead);
        daily.MapPost(
                "/items",
                async (AddDailyItemRequest request, ClaimsPrincipal user, DailyService service, CancellationToken cancellationToken) =>
                    await Write(user, async (userId, clientId) =>
                    {
                        if (request.IssueId is Guid issueId)
                        {
                            if (!string.IsNullOrWhiteSpace(request.CustomText))
                            {
                                throw new ProtocolException("A Daily Item is either one Issue or custom text, never both.");
                            }

                            return await service.AddIssueAsync(
                                request.Date, issueId, request.Rank, userId, userId, clientId, cancellationToken);
                        }

                        return await service.AddItemAsync(
                            request.Date, request.CustomText ?? string.Empty, request.Rank, userId, userId, clientId, cancellationToken);
                    }, created: true))
            .RequireAuthorization(CompanionPermissions.DailyManage);
        daily.MapPost(
                "/items/{id:guid}/complete",
                async (Guid id, ClaimsPrincipal user, DailyService service, CancellationToken cancellationToken) =>
                    await Write(user, (userId, clientId) =>
                        service.CompleteAsync(id, userId, clientId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.DailyManage);
        daily.MapPost(
                "/items/{id:guid}/remove",
                async (Guid id, ClaimsPrincipal user, DailyService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    var removed = await service.RemoveAsync(id, userId, clientId, cancellationToken);
                    return removed is null ? Results.NotFound() : Results.NoContent();
                })
            .RequireAuthorization(CompanionPermissions.DailyManage);
    }

    private static async Task<IResult> Write(
        ClaimsPrincipal user,
        Func<string, Guid?, Task<DailyItemDto>> action,
        bool created = false)
    {
        try
        {
            var (userId, clientId) = ProjectService.ActorFrom(user);
            var result = await action(userId, clientId);
            return created
                ? Results.Created($"/api/v1/daily/items/{result.Id}", result)
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
}

public sealed record AddDailyItemRequest(
    [Required] DateOnly Date,
    Guid? IssueId,
    [StringLength(500)] string? CustomText,
    int? Rank);
