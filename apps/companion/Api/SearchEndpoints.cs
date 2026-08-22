using EnterpriseStarter.Companion.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseStarter.Companion.Api;

public static class SearchEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/search",
                async (
                    string? q,
                    string? type,
                    Guid? projectId,
                    string? tag,
                    SearchService service,
                    CancellationToken cancellationToken,
                    bool archived = false) =>
                    Results.Ok(await service.SearchAsync(q ?? string.Empty, type, projectId, tag, archived, cancellationToken)))
            .RequireAuthorization(CompanionPermissions.SearchRead);
    }
}
