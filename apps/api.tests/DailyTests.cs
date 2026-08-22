using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseStarter.Companion.Application;
using EnterpriseStarter.Companion.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EnterpriseStarter.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class DailyTests(CustomWebApplicationFactory factory)
{
    private const string AdminEmail = "admin@enterprisestarter.local";
    private const string AdminPassword = "AdminPassword123!";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly DateOnly Today = new(2026, 8, 22);
    private static readonly DateOnly ThreeDaysAgo = new(2026, 8, 19);
    private static readonly DateOnly EightDaysAgo = new(2026, 8, 14);

    [Fact]
    public async Task CompletingDailyItem_LeavesIssueStatusUnchanged()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var projectId = await BootstrapProjectIdAsync(client);
            var createdIssue = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/projects/{projectId}/issues",
                new { title = "Stay active", status = IssueStatus.Ready });
            Assert.Equal(HttpStatusCode.Created, createdIssue.StatusCode);
            var issue = await createdIssue.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);
            Assert.NotNull(issue);

            var started = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/issues/{issue.Id}/start",
                new { expectedVersion = issue.Version });
            Assert.Equal(HttpStatusCode.OK, started.StatusCode);
            var active = await started.Content.ReadFromJsonAsync<IssueDto>(JsonOptions);
            Assert.Equal(IssueStatus.Active, active!.Status);

            var added = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/daily/items",
                new { date = Today, issueId = active.Id });
            Assert.Equal(HttpStatusCode.Created, added.StatusCode);
            var dailyItem = await added.Content.ReadFromJsonAsync<DailyItemDto>(JsonOptions);
            Assert.NotNull(dailyItem);
            Assert.Equal(active.Id, dailyItem.IssueId);

            var completed = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                $"/api/v1/daily/items/{dailyItem.Id}/complete",
                null);
            Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
            var doneItem = await completed.Content.ReadFromJsonAsync<DailyItemDto>(JsonOptions);
            Assert.NotNull(doneItem);
            Assert.NotNull(doneItem.CompletedAt);
            Assert.Equal(IssueStatus.Active, doneItem.IssueStatus);

            var after = await client.GetFromJsonAsync<IssueDto>($"/api/v1/issues/{active.Id}", JsonOptions);
            Assert.Equal(IssueStatus.Active, after!.Status);
            Assert.Equal(active.Version, after.Version);
            Assert.Equal(active.AssigneeUserId, after.AssigneeUserId);
            Assert.Null(after.Resolution);
        }
    }

    [Fact]
    public async Task IncompleteItems_StayOnOriginalDate_AndAppearInCarryover()
    {
        await factory.ResetDatabaseAsync();
        var (client, csrf) = await CreateReadyAdminAsync();
        using (client)
        {
            var leftover = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/daily/items",
                new { date = ThreeDaysAgo, customText = "Still open from Thursday" });
            Assert.Equal(HttpStatusCode.Created, leftover.StatusCode);
            var openItem = await leftover.Content.ReadFromJsonAsync<DailyItemDto>(JsonOptions);
            Assert.Equal(ThreeDaysAgo, openItem!.Date);

            var finished = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/daily/items",
                new { date = ThreeDaysAgo, customText = "Already done Thursday" });
            var finishedItem = await finished.Content.ReadFromJsonAsync<DailyItemDto>(JsonOptions);
            Assert.Equal(
                HttpStatusCode.OK,
                (await MutateAsync(
                    client,
                    csrf,
                    HttpMethod.Post,
                    $"/api/v1/daily/items/{finishedItem!.Id}/complete",
                    null)).StatusCode);

            var tooOld = await MutateAsync(
                client,
                csrf,
                HttpMethod.Post,
                "/api/v1/daily/items",
                new { date = EightDaysAgo, customText = "Outside the window" });
            Assert.Equal(HttpStatusCode.Created, tooOld.StatusCode);

            var today = await client.GetFromJsonAsync<DailyDto>($"/api/v1/daily?date={Today:yyyy-MM-dd}", JsonOptions);
            Assert.NotNull(today);
            Assert.Equal(Today, today.Date);
            Assert.Empty(today.Items);
            Assert.Contains(today.Carryover, item => item.Id == openItem.Id && item.Date == ThreeDaysAgo);
            Assert.DoesNotContain(today.Carryover, item => item.Id == finishedItem.Id);
            Assert.DoesNotContain(today.Carryover, item => item.Date == EightDaysAgo);

            var originalDay = await client.GetFromJsonAsync<DailyDto>(
                $"/api/v1/daily?date={ThreeDaysAgo:yyyy-MM-dd}",
                JsonOptions);
            Assert.Contains(originalDay!.Items, item => item.Id == openItem.Id && item.Date == ThreeDaysAgo);
            Assert.Contains(originalDay.Items, item => item.Id == finishedItem.Id && item.CompletedAt is not null);
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

    private sealed record CsrfDto(string HeaderName, string RequestToken);
}
