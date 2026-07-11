using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PagueVeloz.Infrastructure.Observability.HealthChecks;

namespace PagueVeloz.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IHostEnvironment _environment;

    public HealthController(HealthCheckService healthCheckService, IHostEnvironment environment)
    {
        _healthCheckService = healthCheckService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        var response = HealthCheckResponseWriter.BuildResponse(_environment, report);
        return Ok(response);
    }
}
