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
public sealed class SessionActivityTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task SecondOpenSession_ForSameActorAndProject_IsRejected()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var first = await MutateAsync(client, csrf, HttpMethod.Post, $"/api/v1/projects/{projectId}/sessions", null);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await MutateAsync(client, csrf, HttpMethod.Post, $"/api/v1/projects/{projectId}/sessions", null);
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }
    }

    [Fact]
    public async Task Finish_WritesActivitySummary_AndLeavesIssueClaim()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var created = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/projects/{projectId}/issues",
                new { title = "Keep the claim", status = IssueStatus.Ready })).Content
                .ReadFromJsonAsync<IssueDto>(JsonOptions);
            var startedIssue = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{created!.Id}/start",
                new { expectedVersion = created.Version })).Content
                .ReadFromJsonAsync<IssueDto>(JsonOptions);
            Assert.Equal(IssueStatus.Active, startedIssue!.Status);
            Assert.False(string.IsNullOrWhiteSpace(startedIssue.AssigneeUserId));

            var session = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/projects/{projectId}/sessions",
                null)).Content.ReadFromJsonAsync<SessionDto>(JsonOptions);

            var finished = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/sessions/{session!.Id}/finish",
                new { summary = "Shipped the importer notes." });
            Assert.Equal(HttpStatusCode.OK, finished.StatusCode);
            var closed = await finished.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);
            Assert.NotNull(closed!.FinishedAt);
            Assert.Equal("Shipped the importer notes.", closed.Summary);

            var after = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{startedIssue.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Active, after!.Status);
            Assert.Equal(startedIssue.AssigneeUserId, after.AssigneeUserId);
            Assert.Equal(startedIssue.Version, after.Version);

            var events = await client.GetFromJsonAsync<List<ActivityDto>>(
                $"/api/v1/activity?projectId={projectId}&recordType=Session",
                JsonOptions);
            Assert.Contains(events!, item =>
                item.ActionType == "finished"
                && item.RecordId == session.Id
                && item.Summary == "Shipped the importer notes."
                && item.ActorAiClientId is null);
        }
    }

    [Fact]
    public async Task Activity_AttributesAiClientDistinctlyFromOwner()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var createdClient = await (await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/ai-clients",
                new { name = "Cursor" })).Content.ReadFromJsonAsync<AiClientCreated>(JsonOptions);
            Assert.NotNull(createdClient);

            using var mcp = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
            var mcpSession = await InitializeMcpAsync(mcp, createdClient.Secret);
            var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 2,
                ["method"] = "tools/call",
                ["params"] = new Dictionary<string, object?>
                {
                    ["name"] = "issues_create",
                    ["arguments"] = new Dictionary<string, object?>
                    {
                        ["projectId"] = projectId.ToString(),
                        ["title"] = "MCP attributed work",
                        ["status"] = "Ready"
                    }
                }
            });
            Assert.Equal(HttpStatusCode.OK, (await PostMcpAsync(mcp, createdClient.Secret, mcpSession, payload)).StatusCode);

            var events = await client.GetFromJsonAsync<List<ActivityDto>>(
                $"/api/v1/activity?projectId={projectId}&recordType=Issue",
                JsonOptions);
            var created = Assert.Single(events!, item => item.ActionType == "created" && item.Summary.Contains("MCP attributed work", StringComparison.Ordinal));
            Assert.Equal(createdClient.Id, created.ActorAiClientId);
            Assert.False(string.IsNullOrWhiteSpace(created.ActorUserId));
            Assert.NotEqual(createdClient.Id.ToString(), created.ActorUserId);
        }
    }

    private async Task<Guid> BootstrapProjectIdAsync(HttpClient client)
    {
        var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/v1/projects", JsonOptions);
        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
        return projects[0].Id;
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
