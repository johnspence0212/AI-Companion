using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class DocumentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var documents = endpoints.MapGroup("/api/v1/documents");
        documents.MapGet("/", async (DocumentService service, CancellationToken cancellationToken, bool archived = false) =>
                Results.Ok(await service.ListAsync(archived, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.DocumentsRead);
        documents.MapGet(
                "/{idOrSlug}",
                async (string idOrSlug, DocumentService service, CancellationToken cancellationToken) =>
                {
                    var document = await service.GetAsync(idOrSlug, cancellationToken);
                    return document is null ? Results.NotFound() : Results.Ok(document);
                })
            .RequireAuthorization(CompanionPermissions.DocumentsRead);
        documents.MapGet(
                "/{id:guid}/revisions",
                async (Guid id, DocumentService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.ListRevisionsAsync(id, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.DocumentsRead);
        documents.MapPost(
                "/",
                async (CreateDocumentRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(request.Title))
                    {
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["title"] = ["Title is required."]
                        });
                    }

                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    try
                    {
                        var created = await service.CreateAsync(
                            userId, userId, clientId, request.Title, request.Body, request.FolderId,
                            request.TemplateId, request.Slug, request.ProjectIds, request.Tags, cancellationToken,
                            request.ParentDocumentId);
                        return Results.Created($"/api/v1/documents/{created.Id}", created);
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                })
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPut(
                "/{idOrSlug}",
                async (string idOrSlug, SaveDocumentRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                    await Write(user, service, () =>
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        return service.SaveAsync(
                            idOrSlug, userId, clientId, request.ExpectedRevisionId, request.Title, request.Body, cancellationToken);
                    }))
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPost(
                "/{idOrSlug}/append",
                async (string idOrSlug, AppendDocumentRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                    await Write(user, service, () =>
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        return service.AppendAsync(
                            idOrSlug, userId, clientId, request.ExpectedRevisionId, request.Markdown, cancellationToken);
                    }))
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPost(
                "/{idOrSlug}/restore",
                async (string idOrSlug, RestoreDocumentRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                    await Write(user, service, () =>
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        return service.RestoreAsync(
                            idOrSlug, userId, clientId, request.ExpectedRevisionId, request.RevisionId, cancellationToken);
                    }))
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPost(
                "/{id:guid}/archive",
                async (Guid id, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    var archived = await service.ArchiveAsync(id, userId, clientId, cancellationToken);
                    return archived is null ? Results.NotFound() : Results.NoContent();
                })
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPost(
                "/{id:guid}/move",
                async (Guid id, MoveDocumentRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                    await Write(user, service, () =>
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        return service.MoveAsync(id, request.FolderId, userId, clientId, cancellationToken);
                    }))
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPost(
                "/{id:guid}/links/{projectId:guid}",
                async (Guid id, Guid projectId, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    return await service.LinkProjectAsync(id, projectId, userId, clientId, cancellationToken) switch
                    {
                        null => Results.NoContent(),
                        "context-not-linkable" => Results.Conflict(),
                        _ => Results.NotFound()
                    };
                })
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapDelete(
                "/{id:guid}/links/{projectId:guid}",
                async (Guid id, Guid projectId, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                {
                    var (userId, clientId) = ProjectService.ActorFrom(user);
                    return await service.UnlinkProjectAsync(id, projectId, userId, clientId, cancellationToken) is null
                        ? Results.NoContent()
                        : Results.NotFound();
                })
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
        documents.MapPost(
                "/{id:guid}/tags",
                async (Guid id, TagDocumentRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                    await Write(user, service, () =>
                    {
                        var (userId, clientId) = ProjectService.ActorFrom(user);
                        return service.AddTagAsync(id, request.Name, userId, userId, clientId, cancellationToken);
                    }))
            .RequireAuthorization(CompanionPermissions.TagsManage);

        var folders = endpoints.MapGroup("/api/v1/folders");
        folders.MapGet("/", async (DocumentService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListFoldersAsync(cancellationToken)))
            .RequireAuthorization(CompanionPermissions.DocumentsRead);
        folders.MapPost(
                "/",
                async (CreateFolderRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                {
                    var (userId, _) = ProjectService.ActorFrom(user);
                    var created = await service.CreateFolderAsync(userId, request.Name, request.ParentFolderId, cancellationToken);
                    return Results.Created($"/api/v1/folders/{created.Id}", created);
                })
            .RequireAuthorization(CompanionPermissions.DocumentsManage);

        var templates = endpoints.MapGroup("/api/v1/document-templates");
        templates.MapGet("/", async (DocumentService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListTemplatesAsync(cancellationToken)))
            .RequireAuthorization(CompanionPermissions.DocumentsRead);
        templates.MapPost(
                "/",
                async (CreateTemplateRequest request, ClaimsPrincipal user, DocumentService service, CancellationToken cancellationToken) =>
                {
                    var (userId, _) = ProjectService.ActorFrom(user);
                    var created = await service.CreateTemplateAsync(userId, request.Name, request.TitlePattern, request.Body, cancellationToken);
                    return Results.Created($"/api/v1/document-templates/{created.Id}", created);
                })
            .RequireAuthorization(CompanionPermissions.DocumentsManage);
    }

    private static async Task<IResult> Write(ClaimsPrincipal _, DocumentService __, Func<Task<DocumentDto>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ConflictException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: ex.Message,
                extensions: ex.Details is null ? null : new Dictionary<string, object?> { ["current"] = ex.Details });
        }
    }
}

public sealed record CreateDocumentRequest(
    [Required, StringLength(500)] string Title,
    string? Body,
    Guid? FolderId,
    Guid? ParentDocumentId,
    Guid? TemplateId,
    [StringLength(128)] string? Slug,
    IReadOnlyList<Guid>? ProjectIds,
    IReadOnlyList<string>? Tags);

public sealed record SaveDocumentRequest(
    [Required] Guid ExpectedRevisionId,
    [Required] string Body,
    [StringLength(500)] string? Title);

public sealed record AppendDocumentRequest([Required] Guid ExpectedRevisionId, [Required] string Markdown);

public sealed record RestoreDocumentRequest([Required] Guid ExpectedRevisionId, [Required] Guid RevisionId);

public sealed record MoveDocumentRequest(Guid? FolderId);

public sealed record TagDocumentRequest([Required, StringLength(100)] string Name);

public sealed record CreateFolderRequest([Required, StringLength(200)] string Name, Guid? ParentFolderId);

public sealed record CreateTemplateRequest(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(500)] string TitlePattern,
    [Required] string Body);
