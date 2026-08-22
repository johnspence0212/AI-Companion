using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ProjectBootstrapTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task FirstLogin_CreatesExactlyOneBootstrapProject_WithUnlinkedContext()
    {
        await factory.ResetDatabaseAsync();
        var (client, _) = await CreateReadyAdminAsync();
        using (client)
        {
            var first = await client.GetAsync("/api/v1/projects");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var listed = await first.Content.ReadFromJsonAsync<List<ProjectDto>>(JsonOptions);
            Assert.NotNull(listed);
            Assert.Single(listed);
            Assert.Equal(ProjectService.BootstrapName, listed[0].Name);
            Assert.Equal(ProjectService.BootstrapSlug, listed[0].Slug);

            var loginAgain = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = AdminEmail, password = "ReadyAdmin123!" });
            Assert.Equal(HttpStatusCode.OK, loginAgain.StatusCode);
            var second = await (await client.GetAsync("/api/v1/projects")).Content
                .ReadFromJsonAsync<List<ProjectDto>>(JsonOptions);
            Assert.NotNull(second);
            Assert.Single(second);

            var context = await client.GetFromJsonAsync<ProjectContextDto>(
                $"/api/v1/projects/{listed[0].Slug}/context",
                JsonOptions);
            Assert.NotNull(context);
            Assert.Contains("## Goal", context.Body);
            Assert.Contains("## Notes for AI Agents", context.Body);
            Assert.Equal(listed[0].ContextDocumentId, context.DocumentId);

            using var scope = factory.Services.CreateScope();
            var document = await scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>()
                .Set<Document>()
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == listed[0].ContextDocumentId);
            var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, document.OwnerUserId)],
                    "test"))
            };
            var service = scope.ServiceProvider.GetRequiredService<ProjectService>();
            var db = scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>();
            Assert.True(document.IsProjectContext);
            Assert.Null(document.FolderId);
            Assert.Empty(await service.ListLibraryAsync(CancellationToken.None));
            Assert.Equal(
                "context-not-linkable",
                await service.TryLinkDocumentAsync(listed[0].Id, document.Id, CancellationToken.None));
            Assert.False(await db.Set<DocumentProject>().AnyAsync());
        }
    }

    [Fact]
    public async Task CreateProject_AlwaysYieldsPrivateContext_AndMcpCanReadWriteBySlug()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var createdResponse = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/projects",
                new { name = "Fantasy Football" },
                csrf);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = await createdResponse.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
            Assert.NotNull(created);
            Assert.Equal("fantasy-football", created.Slug);

            var context = await client.GetFromJsonAsync<ProjectContextDto>(
                $"/api/v1/projects/{created.Slug}/context",
                JsonOptions);
            Assert.NotNull(context);
            Assert.StartsWith("# Fantasy Football", context.Body);

            var email = $"other-{Guid.NewGuid():N}@test.local";
            var user = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/users",
                new
                {
                    email,
                    displayName = "Other",
                    temporaryPassword = "TemporaryUser123!",
                    roles = new[] { "Member" }
                },
                csrf);
            Assert.Equal(HttpStatusCode.Created, user.StatusCode);
            using var other = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
            Assert.Equal(
                HttpStatusCode.OK,
                (await other.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    new { email, password = "TemporaryUser123!" })).StatusCode);
            var otherCsrf = await GetCsrfAsync(other);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendAsync(
                    other,
                    HttpMethod.Post,
                    "/api/v1/auth/change-password",
                    new { currentPassword = "TemporaryUser123!", newPassword = "ReadyMember123!" },
                    otherCsrf)).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await other.GetAsync($"/api/v1/projects/{created.Id}")).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await other.GetAsync($"/api/v1/projects/{created.Slug}/context")).StatusCode);

            var secretResponse = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/ai-clients",
                new { name = "Cursor" },
                csrf);
            var secret = (await secretResponse.Content.ReadFromJsonAsync<CreatedAiClientDto>(JsonOptions))!.Secret;
            using var mcp = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var session = await InitializeMcpAsync(mcp, secret);

            var read = await PostMcpAsync(
                mcp,
                secret,
                session,
                """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"context_get","arguments":{"idOrSlug":"fantasy-football"}}}""");
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            var readBody = await read.Content.ReadAsStringAsync();
            Assert.Contains("Fantasy Football", readBody, StringComparison.Ordinal);

            var updatedBody = """
                # Fantasy Football

                ## Goal
                Win the league.
                """;
            var writePayload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 3,
                ["method"] = "tools/call",
                ["params"] = new Dictionary<string, object?>
                {
                    ["name"] = "context_update",
                    ["arguments"] = new Dictionary<string, object?>
                    {
                        ["idOrSlug"] = "fantasy-football",
                        ["expectedRevisionId"] = context.RevisionId,
                        ["body"] = updatedBody
                    }
                }
            });
            var write = await PostMcpAsync(mcp, secret, session, writePayload);
            Assert.Equal(HttpStatusCode.OK, write.StatusCode);
            Assert.Contains("Win the league", await write.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            var resource = await PostMcpAsync(
                mcp,
                secret,
                session,
                """{"jsonrpc":"2.0","id":4,"method":"resources/read","params":{"uri":"companion://projects/fantasy-football/context"}}""");
            Assert.Equal(HttpStatusCode.OK, resource.StatusCode);
            Assert.Contains("Win the league", await resource.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            var current = await PostMcpAsync(
                mcp,
                secret,
                session,
                """{"jsonrpc":"2.0","id":5,"method":"resources/read","params":{"uri":"companion://current-project/context"}}""");
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);
            Assert.Contains("## Goal", await current.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
    }

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private async Task<(HttpClient Client, CsrfDto Csrf)> CreateReadyAdminAsync()
    {
        var client = CreateClient();
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { email = AdminEmail, password = AdminPassword })).StatusCode);
        var csrf = await GetCsrfAsync(client);
        var changed = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/change-password",
            new { currentPassword = AdminPassword, newPassword = "ReadyAdmin123!" },
            csrf);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        return (client, await GetCsrfAsync(client));
    }

    private static async Task<CsrfDto> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CsrfDto>(JsonOptions))!;
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body,
        CsrfDto csrf)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(csrf.HeaderName, csrf.RequestToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    private static async Task<string?> InitializeMcpAsync(HttpClient client, string bearer)
    {
        var response = await PostMcpAsync(
            client,
            bearer,
            session: null,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"tests","version":"1.0"}}}""");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response.Headers.TryGetValues("Mcp-Session-Id", out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private static Task<HttpResponseMessage> PostMcpAsync(
        HttpClient client,
        string bearer,
        string? session,
        string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (!string.IsNullOrWhiteSpace(session))
        {
            request.Headers.TryAddWithoutValidation("Mcp-Session-Id", session);
        }

        return client.SendAsync(request);
    }

    private sealed record CsrfDto(string HeaderName, string RequestToken);

    private sealed record CreatedAiClientDto(string Secret);
}
