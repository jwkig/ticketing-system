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
}
