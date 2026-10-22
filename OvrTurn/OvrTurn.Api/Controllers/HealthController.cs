using Microsoft.AspNetCore.Mvc;

namespace OvrTurn.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Liveness probe. Returns 200 OK when the service process is alive.
    /// Does NOT check downstream dependencies (DB, external APIs) — that is readiness, a separate concern.
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "OvrTurn.Api",
        timestamp = DateTimeOffset.UtcNow
    });
}