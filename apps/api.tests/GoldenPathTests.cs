using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class GoldenPathTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private const string HttpFenceToken = "kalshigoldenhttp99";
    private const string McpFenceToken = "kalshigoldenmcp99";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly Today = new(2026, 8, 23);

    private static string FencedBody(string token) =>
        $$"""
        # Notes

        ```python
        def greet(name):
            return f"hello {name}"
        {{token}}
        ```
        """;

    [Fact]
    public async Task LockedV1GoldenPath_PassesThroughHttpAndMcp()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var httpFence = FencedBody(HttpFenceToken);
            var mcpFence = FencedBody(McpFenceToken);

            var createdProject = await MutateAsync(client, csrf, HttpMethod.Post, "/api/v1/projects", new { name = "Golden Path" });
            Assert.Equal(HttpStatusCode.Created, createdProject.StatusCode);
            var project = await createdProject.Content.ReadFromJsonAsync<ProjectDto>(JsonOptions);
            Assert.NotNull(project);

            var context = await client.GetFromJsonAsync<ProjectContextDto>($"/api/v1/projects/{project.Id}/context", JsonOptions);
            Assert.NotNull(context);
            var savedContext = await MutateAsync(
                client,
                csrf,
                HttpMethod.Put,
                $"/api/v1/projects/{project.Id}/context",
                new { expectedRevisionId = context.RevisionId, title = "Golden Context", body = httpFence });
            Assert.Equal(HttpStatusCode.OK, savedContext.StatusCode);
            var contextAfter = await client.GetFromJsonAsync<ProjectContextDto>($"/api/v1/projects/{project.Id}/context", JsonOptions);
            Assert.Equal(httpFence, contextAfter!.Body);

            var createdDocument = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/documents",
                new { title = "HTTP fence notes", body = httpFence, slug = "http-fence-notes" });
            Assert.Equal(HttpStatusCode.Created, createdDocument.StatusCode);
            var document = await createdDocument.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);
            Assert.Equal(httpFence, document!.Body);
            var originalRevision = document.RevisionId;

            var updatedDocument = await MutateAsync(
                client,
                csrf,
                HttpMethod.Put,
                $"/api/v1/documents/{document.Id}",
                new { expectedRevisionId = document.RevisionId, title = "HTTP fence notes", body = httpFence + "\nMore.\n" });
            Assert.Equal(HttpStatusCode.OK, updatedDocument.StatusCode);
            var saved = await updatedDocument.Content.ReadFromJsonAsync<DocumentDto>(JsonOptions);

            var restoredDocument = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/documents/{document.Id}/restore",
                new { expectedRevisionId = saved!.RevisionId, revisionId = originalRevision });
            Assert.Equal(HttpStatusCode.OK, restoredDocument.StatusCode);
            var restored = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{document.Id}", JsonOptions);
            Assert.Equal(httpFence, restored!.Body);
            Assert.Contains("```python", restored.Body);

            var httpSearch = await client.GetFromJsonAsync<SearchResultsDto>(
                $"/api/v1/search?q={HttpFenceToken}&type=document",
                JsonOptions);
            Assert.Contains(httpSearch!.Documents, hit => hit.Id == document.Id);

            var createdClient = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/ai-clients",
                new { name = "Golden Path Client" })).Content.ReadFromJsonAsync<AiClientCreated>(JsonOptions);
            Assert.NotNull(createdClient);

            using var mcp = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var mcpSession = await InitializeMcpAsync(mcp, createdClient.Secret);
            var mcpCall = 2;

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "projects_create", new()
            {
                ["name"] = "MCP Golden Path"
            });
            var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/v1/projects", JsonOptions);
            var mcpProject = Assert.Single(projects!, item => item.Name == "MCP Golden Path");

            var mcpContext = await client.GetFromJsonAsync<ProjectContextDto>(
                $"/api/v1/projects/{mcpProject.Id}/context",
                JsonOptions);
            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "context_update", new()
            {
                ["idOrSlug"] = mcpProject.Id.ToString(),
                ["expectedRevisionId"] = mcpContext!.RevisionId.ToString(),
                ["title"] = "MCP Context",
                ["body"] = mcpFence
            });
            var mcpContextAfter = await client.GetFromJsonAsync<ProjectContextDto>(
                $"/api/v1/projects/{mcpProject.Id}/context",
                JsonOptions);
            Assert.Equal(mcpFence, mcpContextAfter!.Body);

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "documents_create", new()
            {
                ["title"] = "MCP fence notes",
                ["body"] = mcpFence,
                ["slug"] = "mcp-fence-notes"
            });
            var mcpDocument = await client.GetFromJsonAsync<DocumentDto>("/api/v1/documents/mcp-fence-notes", JsonOptions);
            Assert.Equal(mcpFence, mcpDocument!.Body);
            var mcpOriginalRevision = mcpDocument.RevisionId;

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "documents_update", new()
            {
                ["idOrSlug"] = mcpDocument.Id.ToString(),
                ["expectedRevisionId"] = mcpDocument.RevisionId.ToString(),
                ["title"] = "MCP fence notes",
                ["body"] = mcpFence + "\nMore.\n"
            });
            var mcpUpdated = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{mcpDocument.Id}", JsonOptions);
            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "documents_restore", new()
            {
                ["idOrSlug"] = mcpDocument.Id.ToString(),
                ["expectedRevisionId"] = mcpUpdated!.RevisionId.ToString(),
                ["revisionId"] = mcpOriginalRevision.ToString()
            });
            var mcpRestored = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{mcpDocument.Id}", JsonOptions);
            Assert.Equal(mcpFence, mcpRestored!.Body);

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "search_query", new()
            {
                ["q"] = McpFenceToken,
                ["type"] = "document"
            });
            var mcpSearch = await client.GetFromJsonAsync<SearchResultsDto>(
                $"/api/v1/search?q={McpFenceToken}&type=document",
                JsonOptions);
            Assert.Contains(mcpSearch!.Documents, hit => hit.Id == mcpDocument.Id);
            var afterSearch = await client.GetFromJsonAsync<DocumentDto>($"/api/v1/documents/{mcpDocument.Id}", JsonOptions);
            Assert.Equal(mcpFence, afterSearch!.Body);

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "issues_create", new()
            {
                ["projectId"] = project.Id.ToString(),
                ["title"] = "Walk the protocol",
                ["status"] = "Ready"
            });
            var issues = await client.GetFromJsonAsync<List<IssueDto>>($"/api/v1/projects/{project.Id}/issues", JsonOptions);
            var protocol = Assert.Single(issues!, item => item.Title == "Walk the protocol");
            Assert.Equal(IssueStatus.Ready, protocol.Status);

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "issues_start", new()
            {
                ["id"] = protocol.Id.ToString(),
                ["expectedVersion"] = protocol.Version
            });
            var active = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{protocol.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Active, active!.Status);

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "issues_block", new()
            {
                ["id"] = active.Id.ToString(),
                ["expectedVersion"] = active.Version,
                ["reason"] = "Waiting on review."
            });
            var blocked = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{protocol.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Blocked, blocked!.Status);

            var unblocked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{blocked.Id}/unblock",
                new { expectedVersion = blocked.Version });
            Assert.Equal(HttpStatusCode.OK, unblocked.StatusCode);
            var readyAgain = await unblocked.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);

            await CallMcpAsync(mcp, createdClient.Secret, mcpSession, mcpCall++, "issues_complete", new()
            {
                ["id"] = readyAgain!.Id.ToString(),
                ["expectedVersion"] = readyAgain.Version,
                ["resolution"] = "Shipped the golden path."
            });
            var done = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{protocol.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Done, done!.Status);
            Assert.Equal("Shipped the golden path.", done.Resolution);

            var dailyIssue = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/projects/{project.Id}/issues",
                new { title = "Stay active after Daily", status = IssueStatus.Ready })).Content
                .ReadFromJsonAsync<IssueDto>(JsonOptions);
            var startedDailyIssue = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{dailyIssue!.Id}/start",
                new { expectedVersion = dailyIssue.Version })).Content
                .ReadFromJsonAsync<IssueDto>(JsonOptions);
            var dailyItem = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/daily/items",
                new { date = Today, issueId = startedDailyIssue!.Id })).Content
                .ReadFromJsonAsync<DailyItemDto>(JsonOptions);
            var completedDaily = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/daily/items/{dailyItem!.Id}/complete",
                null);
            Assert.Equal(HttpStatusCode.OK, completedDaily.StatusCode);
            var afterDaily = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{startedDailyIssue.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Active, afterDaily!.Status);
            Assert.Equal(startedDailyIssue.Version, afterDaily.Version);
            Assert.Null(afterDaily.Resolution);

            var captured = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/inbox",
                new { text = "Inbox thought becomes a Document." })).Content
                .ReadFromJsonAsync<InboxItemDto>(JsonOptions);
            var processed = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/inbox/{captured!.Id}/process",
                new { createDocument = true, title = "Inbox document" });
            Assert.Equal(HttpStatusCode.OK, processed.StatusCode);
            var asDocument = await processed.Content.ReadFromJsonAsync<InboxItemDto>(JsonOptions);
            var inboxDocument = await client.GetFromJsonAsync<DocumentDto>(
                $"/api/v1/documents/{asDocument!.DocumentId}",
                JsonOptions);
            Assert.Equal("Inbox document", inboxDocument!.Title);
            Assert.Equal("Inbox thought becomes a Document.", inboxDocument.Body);

            var session = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/projects/{project.Id}/sessions",
                null)).Content.ReadFromJsonAsync<SessionDto>(JsonOptions);
            var finished = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/sessions/{session!.Id}/finish",
                new { summary = "Finished the golden path session." });
            Assert.Equal(HttpStatusCode.OK, finished.StatusCode);
            var sessionEvents = await client.GetFromJsonAsync<List<ActivityDto>>(
                $"/api/v1/activity?projectId={project.Id}&recordType=Session",
                JsonOptions);
            Assert.Contains(sessionEvents!, item =>
                item.ActionType == "finished"
                && item.RecordId == session.Id
                && item.Summary == "Finished the golden path session.");

            var issueEvents = await client.GetFromJsonAsync<List<ActivityDto>>(
                $"/api/v1/activity?projectId={project.Id}&recordType=Issue",
                JsonOptions);
            var createdByClient = Assert.Single(
                issueEvents!,
                item => item.ActionType == "created" && item.Summary.Contains("Walk the protocol", StringComparison.Ordinal));
            Assert.Equal(createdClient.Id, createdByClient.ActorAiClientId);
            Assert.False(string.IsNullOrWhiteSpace(createdByClient.ActorUserId));
            Assert.NotEqual(createdClient.Id.ToString(), createdByClient.ActorUserId);

            var email = $"owner-{Guid.NewGuid():N}@test.local";
            var createdUser = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/users",
                new
                {
                    email,
                    displayName = "Second Owner",
                    temporaryPassword = "TemporaryUser123!",
                    roles = new[] { "Member" }
                });
            Assert.Equal(HttpStatusCode.Created, createdUser.StatusCode);

            using var member = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
            Assert.Equal(
                HttpStatusCode.OK,
                (await member.PostAsJsonAsync(
                    "/api/v1/auth/login",
                    new { email, password = "TemporaryUser123!" })).StatusCode);
            var memberCsrf = await GetCsrfAsync(member);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await MutateAsync(
                    member,
                    memberCsrf,
                    HttpMethod.Post,
                    "/api/v1/auth/change-password",
                    new { currentPassword = "TemporaryUser123!", newPassword = "ReadyMember123!" })).StatusCode);

            Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/projects/{project.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await member.GetAsync($"/api/v1/documents/{document.Id}")).StatusCode);
            var foreignSearch = await member.GetFromJsonAsync<SearchResultsDto>(
                $"/api/v1/search?q={HttpFenceToken}",
                JsonOptions);
            Assert.Empty(foreignSearch!.Documents);
            Assert.Empty(foreignSearch.Projects);
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
            (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/auth/change-password",
                new { currentPassword = AdminPassword, newPassword = "ReadyAdmin123!" })).StatusCode);
        return (client, await GetCsrfAsync(client));
    }

    private static async Task<CsrfDto> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CsrfDto>(JsonOptions))!;
    }

    private static Task<HttpResponseMessage> MutateAsync(
        HttpClient client,
        CsrfDto csrf,
        HttpMethod method,
        string path,
        object? body)
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
            null,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"tests","version":"1.0"}}}""");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response.Headers.TryGetValues("Mcp-Session-Id", out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private static async Task CallMcpAsync(
        HttpClient client,
        string bearer,
        string? session,
        int id,
        string name,
        Dictionary<string, object?> arguments)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = "tools/call",
            ["params"] = new Dictionary<string, object?>
            {
                ["name"] = name,
                ["arguments"] = arguments
            }
        });
        var response = await PostMcpAsync(client, bearer, session, payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
}
