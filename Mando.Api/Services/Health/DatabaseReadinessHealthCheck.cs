using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mando.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Mando.Api.Services.Health;

public class DatabaseReadinessHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseReadinessHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    description: "Database is not reachable.");
            }

            var pendingMigrations = (await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken))
                .ToArray();

            if (pendingMigrations.Length > 0)
            {
                return HealthCheckResult.Unhealthy(
                    description: "Database has pending migrations.",
                    data: new Dictionary<string, object>
                    {
                        ["pendingMigrationsCount"] = pendingMigrations.Length,
                        ["pendingMigrations"] = pendingMigrations
                    });
            }

            return HealthCheckResult.Healthy(
                description: "Database is reachable and schema is up to date.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "Database readiness check failed.",
                exception: ex);
        }
    }
}