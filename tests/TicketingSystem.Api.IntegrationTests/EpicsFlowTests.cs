using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TicketingSystem.Api.IntegrationTests;

public sealed class EpicsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password1!";

    private readonly CustomWebApplicationFactory _factory;

    public EpicsFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    [Fact]
    public async Task GetEpics_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/teams/{Guid.NewGuid()}/epics");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEpic_UnderExistingTeam_Returns201_AndAppearsInList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);

        var create = await client.PostAsJsonAsync(
            $"/api/teams/{teamId}/epics", new { title = "Checkout reliability", description = "Improve checkout." });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Checkout reliability", created.GetProperty("title").GetString());
        Assert.Equal(teamId, created.GetProperty("teamId").GetString());
        Assert.Equal(0, created.GetProperty("ticketCount").GetInt32());

        var list = await client.GetFromJsonAsync<JsonElement>($"/api/teams/{teamId}/epics");
        var titles = list.EnumerateArray().Select(e => e.GetProperty("title").GetString());
        Assert.Contains("Checkout reliability", titles);
    }

    [Fact]
    public async Task CreateEpic_UnderMissingTeam_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/teams/{Guid.NewGuid()}/epics", new { title = "Orphan", description = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEpic_EmptyTitle_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamId}/epics", new { title = "   ", description = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEpic_Returns200WithNewTitle()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);
        var create = await client.PostAsJsonAsync($"/api/teams/{teamId}/epics", new { title = "Old", description = (string?)null });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var update = await client.PutAsJsonAsync($"/api/epics/{id}", new { title = "New", description = "now with desc" });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New", updated.GetProperty("title").GetString());
        Assert.Equal("now with desc", updated.GetProperty("description").GetString());
    }

    [Fact]
    public async Task DeleteEpic_WithoutTickets_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();
        var teamId = await CreateTeamAsync(client);
        var create = await client.PostAsJsonAsync($"/api/teams/{teamId}/epics", new { title = "Temp", description = (string?)null });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var delete = await client.DeleteAsync($"/api/epics/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
