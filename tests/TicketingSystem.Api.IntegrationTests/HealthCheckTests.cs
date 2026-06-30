using System.Net;

namespace TicketingSystem.Api.IntegrationTests;

public sealed class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_WithoutToken_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        // Anonymous access proves the endpoint opts out of the global auth policy.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_WithDatabaseUp_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        // The Testcontainers Postgres instance is running, so readiness passes.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
