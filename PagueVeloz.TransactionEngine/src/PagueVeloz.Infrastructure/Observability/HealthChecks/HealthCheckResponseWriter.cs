using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace PagueVeloz.Infrastructure.Observability.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static object BuildResponse(IHostEnvironment environment, HealthReport report)
    {
        return new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            environment = environment.EnvironmentName,
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };
    }
}
