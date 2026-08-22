using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class SavedViewEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var views = endpoints.MapGroup("/api/v1/views");
        views.MapGet(
                "/",
                async (SavedViewEntityType? entityType, Guid? projectId, SavedViewService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.ListAsync(entityType, projectId, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.ViewsRead);
        views.MapGet(
                "/{id:guid}",
                async (Guid id, SavedViewService service, CancellationToken cancellationToken) =>
                {
                    var view = await service.GetAsync(id, cancellationToken);
                    return view is null ? Results.NotFound() : Results.Ok(view);
                })
            .RequireAuthorization(CompanionPermissions.ViewsRead);
        views.MapPost(
                "/{id:guid}/duplicate",
                async (Guid id, DuplicateSavedViewRequest request, ClaimsPrincipal user, SavedViewService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var (userId, _) = ProjectService.ActorFrom(user);
                        var created = await service.DuplicateAsync(id, userId, request.Name, request.ProjectId, cancellationToken);
                        return Results.Created($"/api/v1/views/{created.Id}", created);
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                    catch (ProtocolException ex)
                    {
                        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                    }
                })
            .RequireAuthorization(CompanionPermissions.ViewsManage);
        views.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateSavedViewRequest request, SavedViewService service, CancellationToken cancellationToken) =>
                {
                    try
                    {
                        return Results.Ok(await service.UpdateAsync(
                            id,
                            request.Name,
                            request.Columns,
                            request.Filters,
                            request.Sort,
                            request.GroupBy,
                            cancellationToken));
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                    catch (ProtocolException ex)
                    {
                        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
                    }
                })
            .RequireAuthorization(CompanionPermissions.ViewsManage);
    }
}

public sealed record DuplicateSavedViewRequest([StringLength(200)] string? Name, Guid? ProjectId);

public sealed record UpdateSavedViewRequest(
    [Required, StringLength(200)] string Name,
    IReadOnlyList<string>? Columns,
    IReadOnlyDictionary<string, string>? Filters,
    IReadOnlyList<SavedViewSortDto>? Sort,
    [StringLength(100)] string? GroupBy);
