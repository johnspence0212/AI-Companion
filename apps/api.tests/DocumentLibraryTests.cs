using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using EnterpriseStarter.Platform;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class DocumentLibraryTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string FencedBody =
        """
        # Notes

        ```python
        def greet(name):
            return f"hello {name}"
        ```
        """;

    [Fact]
    public async Task DocumentFencedCode_SurvivesSaveAndRestore_AndProjectUnlinkDoesNotChangeIt()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var createdResponse = await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/documents",
                new { title = "Code notes", body = FencedBody, slug = "code-notes" },
                csrf);
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = await createdResponse.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
            Assert.NotNull(created);
            Assert.Equal(FencedBody, created.Body);
            Assert.Contains("```python", created.Body);
            Assert.Contains("    return f\"hello {name}\"", created.Body);

            var listed = await client.GetFromJsonAsync<List<DocumentDto>>("/api/v1/documents", JsonOptions);
            Assert.NotNull(listed);
            Assert.DoesNotContain(listed, item => item.Title.EndsWith("Context", StringComparison.Ordinal));

            var originalRevision = created.RevisionId;
            var savedResponse = await SendAsync(
                client,
                HttpMethod.Put,
                $"/api/v1/documents/{created.Id}",
                new { expectedRevisionId = created.RevisionId, title = "Code notes", body = FencedBody + "\nMore.\n" },
                csrf);
            Assert.Equal(HttpStatusCode.OK, savedResponse.StatusCode);
            var saved = await savedResponse.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
            Assert.NotNull(saved);
            Assert.NotEqual(originalRevision, saved.RevisionId);

            var restoredResponse = await SendAsync(
                client,
                HttpMethod.Post,
                $"/api/v1/documents/{created.Id}/restore",
                new { expectedRevisionId = saved.RevisionId, revisionId = originalRevision },
                csrf);
            Assert.Equal(HttpStatusCode.OK, restoredResponse.StatusCode);
            var restored = await restoredResponse.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
            Assert.NotNull(restored);
            Assert.Equal(FencedBody, restored.Body);
            Assert.NotEqual(originalRevision, restored.RevisionId);

            var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/v1/projects", JsonOptions);
            Assert.NotNull(projects);
            var project = projects[0];
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendAsync(
                    client,
                    HttpMethod.Post,
                    $"/api/v1/documents/{created.Id}/links/{project.Id}",
                    null,
                    csrf)).StatusCode);

            var beforeArchive = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{created.Id}", JsonOptions);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await SendAsync(
                    client,
                    HttpMethod.Post,
                    $"/api/v1/projects/{project.Id}/archive",
                    null,
                    csrf)).StatusCode);
            var afterArchive = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{created.Id}", JsonOptions);
            Assert.Equal(beforeArchive!.Body, afterArchive!.Body);
            Assert.Equal(beforeArchive.Title, afterArchive.Title);
            Assert.Equal(beforeArchive.RevisionId, afterArchive.RevisionId);

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await client.SendAsync(DeleteWithCsrf(
                    $"/api/v1/documents/{created.Id}/links/{project.Id}",
                    csrf))).StatusCode);
            var afterUnlink = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{created.Id}", JsonOptions);
            Assert.Equal(FencedBody, afterUnlink!.Body);
            Assert.Empty(afterUnlink.ProjectIds);
        }
    }

    [Fact]
    public async Task Mcp_CanCreateAndRestoreFencedCode()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var secret = (await (await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/ai-clients",
                new { name = "Cursor" },
                csrf)).Content.ReadFromJsonAsync<CreatedAiClientDto>(JsonOptions))!.Secret;
            using var mcp = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var session = await InitializeMcpAsync(mcp, secret);

            var createPayload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 2,
                ["method"] = "tools/call",
                ["params"] = new Dictionary<string, object?>
                {
                    ["name"] = "documents_create",
                    ["arguments"] = new Dictionary<string, object?>
                    {
                        ["title"] = "MCP notes",
                        ["body"] = FencedBody,
                        ["slug"] = "mcp-notes"
                    }
                }
            });
            var created = await PostMcpAsync(mcp, secret, session, createPayload);
            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            var createdText = await created.Content.ReadAsStringAsync();
            Assert.True(
                createdText.Contains("python", StringComparison.Ordinal)
                && createdText.Contains("hello {name}", StringComparison.Ordinal),
                createdText);

            var document = await client.GetFromJsonAsync<DocumentDto>("/api/v1/documents/mcp-notes", JsonOptions);
            Assert.NotNull(document);
            var firstRevision = document.RevisionId;

            var updatePayload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 3,
                ["method"] = "tools/call",
                ["params"] = new Dictionary<string, object?>
                {
                    ["name"] = "documents_update",
                    ["arguments"] = new Dictionary<string, object?>
                    {
                        ["idOrSlug"] = "mcp-notes",
                        ["expectedRevisionId"] = firstRevision,
                        ["body"] = "changed"
                    }
                }
            });
            Assert.Equal(HttpStatusCode.OK, (await PostMcpAsync(mcp, secret, session, updatePayload)).StatusCode);
            document = await client.GetFromJsonAsync<DocumentDto>("/api/v1/documents/mcp-notes", JsonOptions);

            var restorePayload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 4,
                ["method"] = "tools/call",
                ["params"] = new Dictionary<string, object?>
                {
                    ["name"] = "documents_restore",
                    ["arguments"] = new Dictionary<string, object?>
                    {
                        ["idOrSlug"] = "mcp-notes",
                        ["expectedRevisionId"] = document!.RevisionId,
                        ["revisionId"] = firstRevision
                    }
                }
            });
            var restored = await PostMcpAsync(mcp, secret, session, restorePayload);
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
            var afterRestore = await client.GetFromJsonAsync<DocumentDto>("/api/v1/documents/mcp-notes", JsonOptions);
            Assert.Equal(FencedBody, afterRestore!.Body);

            using var scope = factory.Services.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<EnterpriseDbContext>()
                .Set<Document>()
                .IgnoreQueryFilters()
                .Include(item => item.CurrentRevision)
                .SingleAsync(item => item.Slug == "mcp-notes");
            Assert.Equal(FencedBody, stored.CurrentRevision!.Body);
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
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await SendAsync(
                client,
                HttpMethod.Post,
                "/api/v1/auth/change-password",
                new { currentPassword = AdminPassword, newPassword = "ReadyAdmin123!" },
                csrf)).StatusCode);
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

    private static HttpRequestMessage DeleteWithCsrf(string path, CsrfDto csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.TryAddWithoutValidation(csrf.HeaderName, csrf.RequestToken);
        return request;
    }

    private static async Task<string?> InitializeMcpAsync(HttpClient client, string bearer)
    {
        var response = await PostMcpAsync(
            client,
            bearer,
            null,
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
