using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class IssueProtocolTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Ready_Start_Blocked_Unblock_Done_StoresResolution()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var created = await CreateIssueAsync(client, csrf, projectId, "Walk the protocol", IssueStatus.Ready);
            Assert.Equal(IssueStatus.Ready, created.Status);
            Assert.Equal(IssuePriority.Normal, created.Priority);
            Assert.Null(created.AssigneeUserId);

            var started = await MutateAsync(client, csrf, HttpMethod.Post, $"/api/v1/issues/{created.Id}/start", new { expectedVersion = created.Version });
            Assert.Equal(HttpStatusCode.OK, started.StatusCode);
            var active = await ReadIssueAsync(started);
            Assert.Equal(IssueStatus.Active, active.Status);
            Assert.False(string.IsNullOrWhiteSpace(active.AssigneeUserId));

            var blocked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{active.Id}/block",
                new { expectedVersion = active.Version, reason = "Waiting on review." });
            Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
            var waiting = await ReadIssueAsync(blocked);
            Assert.Equal(IssueStatus.Blocked, waiting.Status);
            Assert.Equal("Waiting on review.", waiting.BlockedReason);
            Assert.True(waiting.EffectivelyBlocked);

            var unblocked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{waiting.Id}/unblock",
                new { expectedVersion = waiting.Version });
            Assert.Equal(HttpStatusCode.OK, unblocked.StatusCode);
            var readyAgain = await ReadIssueAsync(unblocked);
            Assert.Equal(IssueStatus.Ready, readyAgain.Status);

            var completed = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{readyAgain.Id}/complete",
                new { expectedVersion = readyAgain.Version, resolution = "Shipped the protocol walk." });
            Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
            var done = await ReadIssueAsync(completed);
            Assert.Equal(IssueStatus.Done, done.Status);
            Assert.Equal("Shipped the protocol walk.", done.Resolution);
            Assert.False(done.EffectivelyBlocked);
        }
    }

    [Fact]
    public async Task StartNext_ReturnsExistingActive_AndGetNextDoesNotClaim()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var ready = await CreateIssueAsync(client, csrf, projectId, "Next candidate", IssueStatus.Ready);

            var peeked = await client.GetFromJsonAsync<IssueDto>($"/api/v1/projects/{projectId}/issues/next", JsonOptions);
            Assert.NotNull(peeked);
            Assert.Equal(ready.Id, peeked.Id);
            Assert.Null(peeked.AssigneeUserId);
            Assert.Equal(IssueStatus.Ready, peeked.Status);

            var afterPeek = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{ready.Id}", JsonOptions);
            Assert.Null(afterPeek!.AssigneeUserId);
            Assert.Equal(IssueStatus.Ready, afterPeek.Status);

            var started = await MutateAsync(client, csrf, HttpMethod.Post, $"/api/v1/projects/{projectId}/issues/start-next", null);
            Assert.Equal(HttpStatusCode.OK, started.StatusCode);
            var active = await ReadIssueAsync(started);
            Assert.Equal(ready.Id, active.Id);
            Assert.Equal(IssueStatus.Active, active.Status);
            Assert.False(string.IsNullOrWhiteSpace(active.AssigneeUserId));

            var again = await MutateAsync(client, csrf, HttpMethod.Post, $"/api/v1/projects/{projectId}/issues/start-next", null);
            Assert.Equal(HttpStatusCode.OK, again.StatusCode);
            var stillActive = await ReadIssueAsync(again);
            Assert.Equal(active.Id, stillActive.Id);
            Assert.Equal(IssueStatus.Active, stillActive.Status);
            Assert.Equal(active.AssigneeUserId, stillActive.AssigneeUserId);
            Assert.Equal(active.Version, stillActive.Version);
        }
    }

    [Fact]
    public async Task EffectivelyBlocked_CannotBecomeDone()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var dependency = await CreateIssueAsync(client, csrf, projectId, "Open dependency", IssueStatus.Ready);
            var blockedByLink = await CreateIssueAsync(client, csrf, projectId, "Depends on other work", IssueStatus.Ready);
            var linked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{blockedByLink.Id}/blockers",
                new { expectedVersion = blockedByLink.Version, blockerIssueId = dependency.Id });
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
            var waitingOnDep = await ReadIssueAsync(linked);
            Assert.True(waitingOnDep.EffectivelyBlocked);

            var completeLinked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{waitingOnDep.Id}/complete",
                new { expectedVersion = waitingOnDep.Version, resolution = "Should not land." });
            Assert.Equal(HttpStatusCode.BadRequest, completeLinked.StatusCode);
            var stillOpen = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{waitingOnDep.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Ready, stillOpen!.Status);
            Assert.Null(stillOpen.Resolution);

            var explicitBlock = await CreateIssueAsync(client, csrf, projectId, "Waiting on humans", IssueStatus.Ready);
            var blocked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{explicitBlock.Id}/block",
                new { expectedVersion = explicitBlock.Version, reason = "Need a decision." });
            Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
            var waiting = await ReadIssueAsync(blocked);

            var completeBlocked = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{waiting.Id}/complete",
                new { expectedVersion = waiting.Version, resolution = "Should not land either." });
            Assert.Equal(HttpStatusCode.BadRequest, completeBlocked.StatusCode);
            var stillBlocked = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{waiting.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Blocked, stillBlocked!.Status);
            Assert.Null(stillBlocked.Resolution);
        }
    }

    private async Task<Guid> BootstrapProjectIdAsync(HttpClient client)
    {
        var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/v1/projects", JsonOptions);
        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
        return projects[0].Id;
    }

    private async Task<IssueDto> CreateIssueAsync(
        HttpClient client,
        CsrfDto csrf,
        Guid projectId,
        string title,
        IssueStatus status)
    {
        var response = await MutateAsync(
            client,
            csrf,
            HttpMethod.Post,
            $"/api/v1/projects/{projectId}/issues",
            new { title, status });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadIssueAsync(response);
    }

    private static async Task<IssueDto> ReadIssueAsync(HttpResponseMessage response)
    {
        var issue = await response.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);
        Assert.NotNull(issue);
        return issue;
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

    private sealed record CsrfDto(string HeaderName, string RequestToken);
}
