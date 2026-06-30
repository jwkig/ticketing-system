using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TicketingSystem.Infrastructure.Persistence;

namespace TicketingSystem.Api.HealthChecks;

/// <summary>
/// Readiness check that verifies the application can reach the database.
/// Kept dependency-free (no extra NuGet package) by probing the existing
/// <see cref="AppDbContext"/> connection directly.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("Database is reachable.")
            : HealthCheckResult.Unhealthy("Database is not reachable.");
    }
}
