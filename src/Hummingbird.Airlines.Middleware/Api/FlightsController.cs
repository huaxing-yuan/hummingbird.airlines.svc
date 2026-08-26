using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Middleware.Soap;
using Microsoft.AspNetCore.Mvc;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>Read-only flight information relayed from the airport flight control system.</summary>
[ApiController]
[Route("api/v1/flights")]
[Produces("application/json")]
public class FlightsController(SoapGateway gateway) : ControllerBase
{
    /// <summary>
    /// Search the flight schedule. Departure instants are computed against the current clock,
    /// so statuses (scheduled / checkInOpen / boarding / departed / cancelled) always reflect "now".
    /// All filters are optional and combinable.
    /// </summary>
    /// <param name="from">IATA departure airport filter, e.g. PEK.</param>
    /// <param name="to">IATA arrival airport filter, e.g. CDG.</param>
    /// <param name="date">UTC departure date filter (yyyy-MM-dd).</param>
    /// <returns>Flights ordered by scheduled departure; empty list when nothing matches.</returns>
    /// <response code="200">Search executed successfully, possibly empty.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Flight>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<Flight>> Search(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] DateOnly? date)
    {
        var departureDate = date?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return Ok(gateway.SearchFlights(from, to, departureDate));
    }

    /// <summary>Get a single flight by its number, including its live operational status.</summary>
    /// <param name="flightNumber">Hummingbird flight number, e.g. HB900 or hb900 (case-insensitive).</param>
    /// <returns>The flight with from/to airports, times, gate and status.</returns>
    /// <response code="200">Flight found.</response>
    /// <response code="404">No flight carries this number (code FLIGHT_NOT_FOUND).</response>
    [HttpGet("{flightNumber}")]
    [ProducesResponseType(typeof(Flight), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<Flight> Get(string flightNumber) => Ok(gateway.GetFlight(flightNumber));
}
