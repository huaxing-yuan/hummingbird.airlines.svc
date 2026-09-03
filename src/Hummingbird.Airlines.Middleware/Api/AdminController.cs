using Hummingbird.Airlines.Middleware.Soap;
using Microsoft.AspNetCore.Mvc;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>
/// Administrative operations. Public, like everything else on this test server:
/// no auth, no sessions. Use with care on the shared live instance.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
public class AdminController(SoapGateway gateway) : ControllerBase
{
    /// <summary>
    /// Reset all in-memory demo data to fresh state. Calls the booking backend over SOAP
    /// (real round-trip): drops every booking, replays the five frozen demo seeds and
    /// restarts the deterministic counters (booking refs from T00001, bag tags from
    /// HB-00000001, boarding sequences from 201).
    /// Global operation: resets the whole server, not a session. No concurrency
    /// guarantees - a reset racing with in-flight calls may interleave. Rate limits
    /// still apply, and chaos drills are bypassed (reset always succeeds).
    /// </summary>
    /// <response code="200">Reset completed; response lists the restored demo refs.</response>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetResult), StatusCodes.Status200OK)]
    public ActionResult<ResetResult> Reset()
    {
        var reseeded = gateway.ResetDemoData();
        return Ok(new ResetResult
        {
            Reset = true,
            ResetAtUtc = DateTime.UtcNow,
            DemoBookings = reseeded.Items.Select(b => b.BookingRef).ToList(),
        });
    }
}
