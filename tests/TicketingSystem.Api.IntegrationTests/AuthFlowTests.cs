using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TicketingSystem.Api.IntegrationTests;

public sealed class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Password1!";

    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task SignUp_NewEmail_Returns201_AndCapturesVerificationToken()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/api/auth/signup", new { email, password = Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(_factory.Email.TokenFor(email)));
    }

    [Fact]
    public async Task SignUp_DuplicateEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        var body = new { email, password = Password };

        var first = await client.PostAsJsonAsync("/api/auth/signup", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/signup", body);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_BeforeVerification_Returns400()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/signup", new { email, password = Password });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_ThenLogin_Returns204Then200WithToken()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/signup", new { email, password = Password });
        var token = _factory.Email.TokenFor(email);
        Assert.NotNull(token);

        var verify = await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(token!)}");
        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200AndUserId()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/signup", new { email, password = Password });
        var token = _factory.Email.TokenFor(email)!;
        await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(token)}");

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var jwt = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(Guid.TryParse(body.GetProperty("userId").GetString(), out _));
    }

    [Fact]
    public async Task SignUp_InvalidPayload_Returns400WithErrorsBody()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/signup",
            new { email = "not-an-email", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
    }
}
