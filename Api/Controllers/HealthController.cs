using Microsoft.AspNetCore.Mvc;

namespace CSpider.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "pong" });
    }
}