using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TicketingSystem.Api.IntegrationTests;

public sealed class TicketsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password1!";

    private readonly CustomWebApplicationFactory _factory;

    public TicketsFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/signup", new { email, password = Password });
        var token = _factory.Email.TokenFor(email)!;
        await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(token)}");
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var jwt = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    private static async Task<string> CreateTeamAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/teams", new { name = $"Team-{Guid.NewGuid():N}" });
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateEpicAsync(HttpClient client, string teamId)
    {
        var create = await client.PostAsJsonAsync(
            $"/api/teams/{teamId}/epics", new { title = $"Epic-{Guid.NewGuid():N}", description = (string?)null });
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> CreateTicketAsync(HttpClient client, string teamId, object body)
    {
        var create = await client.PostAsJsonAsync($"/api/teams/{teamId}/tickets", body);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        return await create.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task GetTickets_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/teams/{Guid.NewGuid()}/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_ForTeamWithoutTickets_Returns200EmptyList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);

        var response = await client.GetAsync($"/api/teams/{teamId}/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, list.ValueKind);
        Assert.Empty(list.EnumerateArray());
    }

    [Fact]
    public async Task CreateTicket_Returns201_AndAppearsInBoardList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);

        var created = await CreateTicketAsync(
            client, teamId, new { type = "bug", title = "Login fails", body = "repro steps", epicId = (string?)null });
        Assert.Equal("bug", created.GetProperty("type").GetString());
        Assert.Equal("new", created.GetProperty("state").GetString());
        Assert.Equal("repro steps", created.GetProperty("body").GetString());

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/teams/{teamId}/tickets");
        var titles = list.EnumerateArray().Select(t => t.GetProperty("title").GetString());
        Assert.Contains("Login fails", titles);
    }

    [Fact]
    public async Task CreateTicket_WithEpicFromAnotherTeam_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamA = await CreateTeamAsync(client);
        var teamB = await CreateTeamAsync(client);
        var epicOnB = await CreateEpicAsync(client, teamB);

        var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamA}/tickets", new { type = "bug", title = "T", body = "B", epicId = epicOnB });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_UnderMissingTeam_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/teams/{Guid.NewGuid()}/tickets", new { type = "bug", title = "T", body = "B", epicId = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_UnknownType_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamId}/tickets", new { type = "banana", title = "T", body = "B", epicId = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTicket_Returns200_WithNewFields()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);
        var created = await CreateTicketAsync(
            client, teamId, new { type = "bug", title = "Old", body = "old", epicId = (string?)null });
        var id = created.GetProperty("id").GetString();

        var update = await client.PutAsJsonAsync(
            $"/api/tickets/{id}", new { type = "feature", title = "New", body = "new body", epicId = (string?)null });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("feature", updated.GetProperty("type").GetString());
        Assert.Equal("New", updated.GetProperty("title").GetString());
        Assert.Equal("new body", updated.GetProperty("body").GetString());
    }

    [Fact]
    public async Task ChangeState_Returns200_AndListReflectsNewState()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);
        var created = await CreateTicketAsync(
            client, teamId, new { type = "bug", title = "Movable", body = "b", epicId = (string?)null });
        var id = created.GetProperty("id").GetString();

        var patch = await client.PatchAsJsonAsync($"/api/tickets/{id}/state", new { state = "in_progress" });

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal("in_progress", (await patch.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("state").GetString());

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/teams/{teamId}/tickets");
        var moved = list.EnumerateArray().Single(t => t.GetProperty("id").GetString() == id);
        Assert.Equal("in_progress", moved.GetProperty("state").GetString());
    }

    [Fact]
    public async Task DeleteTicket_Returns204_AndRemovesFromList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);
        var created = await CreateTicketAsync(
            client, teamId, new { type = "fix", title = "Temp", body = "b", epicId = (string?)null });
        var id = created.GetProperty("id").GetString();

        var delete = await client.DeleteAsync($"/api/tickets/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/teams/{teamId}/tickets");
        Assert.DoesNotContain(list.EnumerateArray(), t => t.GetProperty("id").GetString() == id);
    }

    [Fact]
    public async Task GetTicketById_Returns200_WithBody()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);
        var created = await CreateTicketAsync(
            client, teamId, new { type = "bug", title = "Detail", body = "full body text", epicId = (string?)null });
        var id = created.GetProperty("id").GetString();

        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{id}");

        Assert.Equal("Detail", detail.GetProperty("title").GetString());
        Assert.Equal("full body text", detail.GetProperty("body").GetString());
    }
}
