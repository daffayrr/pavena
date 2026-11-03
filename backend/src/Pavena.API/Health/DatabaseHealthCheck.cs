using Microsoft.Extensions.Diagnostics.HealthChecks;
using Pavena.Infrastructure.Persistence;

namespace Pavena.API.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PavenaDbContext _dbContext;

    public DatabaseHealthCheck(PavenaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is responsive and accessible.")
                : HealthCheckResult.Unhealthy("Database connection could not be established.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database connection failed: {ex.Message}", ex);
        }
    }
}
