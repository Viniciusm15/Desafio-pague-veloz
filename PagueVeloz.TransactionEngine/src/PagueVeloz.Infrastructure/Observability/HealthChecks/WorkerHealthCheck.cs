using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PagueVeloz.Infrastructure.Persistence.Context;

namespace PagueVeloz.Infrastructure.Observability.HealthChecks;

public class WorkerHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;
    private readonly ILogger<WorkerHealthCheck> _logger;

    public WorkerHealthCheck(AppDbContext context, ILogger<WorkerHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var lastProcessed = await _context.OutboxMessages
                .Where(m => m.ProcessedAt != null)
                .OrderByDescending(m => m.ProcessedAt)
                .Select(m => m.ProcessedAt.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastProcessed == default)
                return HealthCheckResult.Degraded("Worker has never processed any message");

            var elapsed = DateTime.UtcNow - lastProcessed;
            var threshold = TimeSpan.FromMinutes(5);

            if (elapsed > threshold)
                return HealthCheckResult.Degraded($"Worker idle for {elapsed.TotalMinutes:F1} minutes");

            return HealthCheckResult.Healthy($"Worker active, last processed at {lastProcessed}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking worker health");
            return HealthCheckResult.Unhealthy("Unable to check worker health");
        }
    }
}
