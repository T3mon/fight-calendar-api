using Microsoft.AspNetCore.Mvc;

namespace FightCalendar.Web.Controllers.Api;

[ApiController]
[Route("api/health")]
public class HealthController(IWebHostEnvironment env) : ControllerBase
{
    // Version bumped by hand on each change - useful for eyeballing which
    // deploy is actually live on a given environment (Render's own logs
    // show this too, but this is a one-glance check without leaving the API).
    private const string Version = "v1";

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        environment = env.EnvironmentName,
        version = Version,
        serverTimeUtc = DateTimeOffset.UtcNow,
    });
}
