using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TicketingSystem.Api.IntegrationTests;

public sealed class TeamsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password1!";

    private readonly CustomWebApplicationFactory _factory;

    public TeamsFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static string NewName() => $"Team-{Guid.NewGuid():N}";

    /// <summary>Signs up, verifies, and logs in a fresh user, returning a bearer-authenticated client.</summary>
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

    [Fact]
    public async Task GetTeams_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/teams");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Returns201_AndAppearsInList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var name = NewName();

        var create = await client.PostAsJsonAsync("/api/teams", new { name });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(name, created.GetProperty("name").GetString());
        Assert.Equal(0, created.GetProperty("ticketCount").GetInt32());
        Assert.Equal(0, created.GetProperty("epicCount").GetInt32());

        var list = await client.GetFromJsonAsync<JsonElement>("/api/teams");
        var names = list.EnumerateArray().Select(t => t.GetProperty("name").GetString());
        Assert.Contains(name, names);
    }

    [Fact]
    public async Task CreateTeam_DuplicateNameCaseInsensitive_Returns409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var name = NewName();
        await client.PostAsJsonAsync("/api/teams", new { name });

        var duplicate = await client.PostAsJsonAsync("/api/teams", new { name = name.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_EmptyName_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/teams", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RenameTeam_Returns200WithNewName()
    {
        var client = await CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/api/teams", new { name = NewName() });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var newName = NewName();
        var rename = await client.PutAsJsonAsync($"/api/teams/{id}", new { name = newName });

        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        var updated = await rename.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(newName, updated.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DeleteTeam_EmptyTeam_Returns204()
    {
        var client = await CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/api/teams", new { name = NewName() });
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var delete = await client.DeleteAsync($"/api/teams/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }
}
