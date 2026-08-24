using System.ComponentModel;
using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace EnterpriseStarter.Companion.Mcp;

[McpServerToolType]
public sealed class DocumentTools(DocumentService documents, IHttpContextAccessor accessor)
{
    [McpServerTool(Name = "documents_list"), Description("List library Documents. Project Context is excluded.")]
    public Task<IReadOnlyList<DocumentDto>> List(CancellationToken cancellationToken) =>
        documents.ListAsync(includeArchived: false, cancellationToken);

    [McpServerTool(Name = "documents_create"), Description("Create a library Document. Body is stored exactly, including fenced code. Pass parentDocumentId to nest it inside another Document.")]
    public Task<DocumentDto> Create(
        [Description("Document title")] string title,
        [Description("Markdown body")] string? body = null,
        [Description("Optional slug")] string? slug = null,
        [Description("Optional folder id")] string? folderId = null,
        [Description("Optional parent Document id")] string? parentDocumentId = null,
        [Description("Optional template id")] string? templateId = null)
    {
        var (userId, clientId) = Actor();
        return documents.CreateAsync(
            userId,
            userId,
            clientId,
            title,
            body,
            ParseGuid(folderId),
            ParseGuid(templateId),
            slug,
            null,
            null,
            CancellationToken.None,
            ParseGuid(parentDocumentId));
    }

    [McpServerTool(Name = "documents_get"), Description("Get a Document by id or slug.")]
    public Task<DocumentDto?> Get([Description("Document id or slug")] string idOrSlug) =>
        documents.GetAsync(idOrSlug, CancellationToken.None);

    [McpServerTool(Name = "documents_update"), Description("Replace the current Document title and Markdown body.")]
    public async Task<object> Update(
        [Description("Document id or slug")] string idOrSlug,
        [Description("Expected current revision id")] string expectedRevisionId,
        [Description("Full Markdown body")] string body,
        [Description("Optional title")] string? title = null)
    {
        try
        {
            var (userId, clientId) = Actor();
            return await documents.SaveAsync(
                idOrSlug, userId, clientId, Guid.Parse(expectedRevisionId), title, body, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    [McpServerTool(Name = "documents_append"), Description("Append Markdown to the current Document body.")]
    public async Task<object> Append(
        [Description("Document id or slug")] string idOrSlug,
        [Description("Expected current revision id")] string expectedRevisionId,
        [Description("Markdown to append")] string markdown)
    {
        try
        {
            var (userId, clientId) = Actor();
            return await documents.AppendAsync(
                idOrSlug, userId, clientId, Guid.Parse(expectedRevisionId), markdown, CancellationToken.None);
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    [McpServerTool(Name = "documents_restore"), Description("Restore a historical Revision by copying it into a new current Revision.")]
    public async Task<object> Restore(
        [Description("Document id or slug")] string idOrSlug,
        [Description("Expected current revision id")] string expectedRevisionId,
        [Description("Historical revision id to restore")] string revisionId)
    {
        try
        {
            var (userId, clientId) = Actor();
            return await documents.RestoreAsync(
                idOrSlug, userId, clientId, Guid.Parse(expectedRevisionId), Guid.Parse(revisionId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    [McpServerTool(Name = "documents_archive"), Description("Archive a Document.")]
    public Task<DocumentDto?> Archive([Description("Document id")] string id)
    {
        var (userId, clientId) = Actor();
        return documents.ArchiveAsync(Guid.Parse(id), userId, clientId, CancellationToken.None);
    }

    [McpServerTool(Name = "documents_move"), Description("Move a Document into a Folder or to the library root.")]
    public Task<DocumentDto> Move(
        [Description("Document id")] string id,
        [Description("Folder id, or omit for root")] string? folderId = null)
    {
        var (userId, clientId) = Actor();
        return documents.MoveAsync(Guid.Parse(id), ParseGuid(folderId), userId, clientId, CancellationToken.None);
    }

    [McpServerTool(Name = "documents_link_project"), Description("Link a Document to a Project.")]
    public async Task<string> LinkProject(
        [Description("Document id")] string documentId,
        [Description("Project id")] string projectId)
    {
        var (userId, clientId) = Actor();
        return await documents.LinkProjectAsync(
            Guid.Parse(documentId), Guid.Parse(projectId), userId, clientId, CancellationToken.None) ?? "ok";
    }

    [McpServerTool(Name = "documents_unlink_project"), Description("Unlink a Document from a Project without changing the Document.")]
    public async Task<string> UnlinkProject(
        [Description("Document id")] string documentId,
        [Description("Project id")] string projectId)
    {
        var (userId, clientId) = Actor();
        return await documents.UnlinkProjectAsync(
            Guid.Parse(documentId), Guid.Parse(projectId), userId, clientId, CancellationToken.None) ?? "ok";
    }

    [McpServerTool(Name = "documents_tag"), Description("Add a user-global Tag to a Document.")]
    public Task<DocumentDto> Tag(
        [Description("Document id")] string documentId,
        [Description("Tag name")] string name)
    {
        var (userId, clientId) = Actor();
        return documents.AddTagAsync(Guid.Parse(documentId), name, userId, userId, clientId, CancellationToken.None);
    }

    private (string UserId, Guid? ClientId) Actor() =>
        ProjectService.ActorFrom(
            accessor.HttpContext?.User
            ?? throw new InvalidOperationException("Authenticated owner required."));

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
