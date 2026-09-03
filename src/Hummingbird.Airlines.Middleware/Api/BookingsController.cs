using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Backend.Services;
using Hummingbird.Airlines.Middleware.Soap;
using Hummingbird.Airlines.Middleware.Translation;
using Microsoft.AspNetCore.Mvc;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>Optional body of POST /bookings/{ref}/check-in. When present it carries the polymorphic bag array
/// that will be registered atomically together with the check-in.</summary>
public sealed class RestCheckInRequest
{
    /// <summary>Polymorphic bag array: at most one checked and one carry-on per passenger. Each item carries a type discriminator.</summary>
    /// <example>[{"type":"checked","weightKg":23,"color":"black","lengthCm":75}, {"type":"carryOn","weightKg":7,"hasLaptop":true}]</example>
    public List<Baggage> Bags { get; set; } = [];
}

[ApiController]
[Route("api/v1/bookings")]
[Produces("application/json")]
public class BookingsController(SoapGateway gateway) : ControllerBase
{
    // ------------------------------------------------------------------ CRUD

    /// <summary>
    /// Create a booking. The backend validates the flight (must exist, not cancelled,
    /// not departed); no seat is assigned until check-in. The flight is identified by
    /// a structured designator: carrier enum (e.g. hb) plus numeric number.
    /// </summary>
    /// <param name="request">Flight designator (carrier + number), cabin class and passenger identity.</param>
    /// <returns>The created booking including its six-character reference and nested flight designator.</returns>
    /// <response code="201">Booking created; Location header points at the new resource.</response>
    /// <response code="400">Malformed body (code INVALID_REQUEST).</response>
    /// <response code="404">Unknown flight (code FLIGHT_NOT_FOUND).</response>
    /// <response code="409">Flight cancelled or already departed (FLIGHT_CANCELLED / FLIGHT_DEPARTED).</response>
    [HttpPost]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<Booking> Create([FromBody] CreateBookingRequest request)
    {
        var booking = gateway.CreateBooking(request);
        return CreatedAtAction(nameof(GetByRef), new { reference = booking.BookingRef }, booking);
    }

    /// <summary>List all bookings of one passenger identified by passport number.</summary>
    /// <param name="passport">Passport number as given during booking creation.</param>
    /// <returns>Bookings ordered by creation time (oldest first) plus a count.</returns>
    /// <response code="200">Search executed; Items may be empty.</response>
    /// <response code="400">The passport query parameter is missing.</response>
    [HttpGet]
    [ProducesResponseType(typeof(BookingListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<BookingListResponse> List([FromQuery] string? passport)
    {
        if (string.IsNullOrWhiteSpace(passport))
        {
            ModelState.AddModelError("passport", "The passport query parameter is required.");
            return ValidationProblem(ModelState);
        }

        var items = gateway.FindBookings(passport);
        return Ok(new BookingListResponse { Items = items, Count = items.Count });
    }

    /// <summary>Get one booking by its six-character reference, including registered bags and nested flight designator.</summary>
    /// <param name="reference">Booking reference, e.g. GZT001 (case-insensitive).</param>
    /// <response code="200">Booking found.</response>
    /// <response code="404">No such booking (BOOKING_NOT_FOUND) - also returned after eviction.</response>
    [HttpGet("{reference}")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<Booking> GetByRef(string reference) => Ok(gateway.GetBooking(reference));

    /// <summary>
    /// Replace the mutable part of a booking: cabin class and passenger identity.
    /// Rejected once the booking has been checked in. Bags stay attached to the booking.
    /// </summary>
    /// <param name="reference">Booking reference to update.</param>
    /// <param name="request">New cabin class and passenger (full replacement, not a patch).</param>
    /// <returns>The updated booking.</returns>
    /// <response code="200">Update applied.</response>
    /// <response code="404">Unknown booking.</response>
    /// <response code="409">Already checked in (ALREADY_CHECKED_IN).</response>
    [HttpPut("{reference}")]
    [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<Booking> Update(string reference, [FromBody] UpdateBookingRequest request) =>
        Ok(gateway.UpdateBooking(reference, request));

    /// <summary>Cancel (delete) a booking. The record is removed immediately;
    /// subsequent GETs answer 404 BOOKING_NOT_FOUND.</summary>
    /// <param name="reference">Booking reference to cancel.</param>
    /// <response code="204">Booking removed.</response>
    /// <response code="404">Unknown booking (idempotency: repeating the call).</response>
    [HttpDelete("{reference}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(string reference)
    {
        gateway.CancelBooking(reference);
        return NoContent();
    }

    // ---------------------------------------------- Check-in & baggage drop

    /// <summary>
    /// One-shot flight registration (check-in) with optional polymorphic bag array.
    /// Assigns seat/gate/boarding sequence and atomically registers the bags.
    /// Fails when less than 30 minutes remain before departure; enforces at most one bag of each type
    /// across the booking. Seat rows depend on cabin class.
    /// </summary>
    /// <param name="reference">Booking reference to check in.</param>
    /// <param name="body">Optional body carrying the polymorphic bag array.</param>
    /// <returns>A boarding pass summary plus the accepted bags and translated warnings.</returns>
    /// <response code="200">Check-in succeeded; booking now carries Seat/Gate/sequence and bags.</response>
    /// <response code="400">Malformed body or weight out of range (INVALID_REQUEST).</response>
    /// <response code="404">Unknown booking.</response>
    /// <response code="409">CHECKIN_CLOSED (&lt; 30 min), ALREADY_CHECKED_IN, BAGGAGE_TYPE_LIMIT, FLIGHT_DEPARTED.</response>
    [HttpPost("{reference}/check-in")]
    [ProducesResponseType(typeof(CheckInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<CheckInResponse> CheckIn(
        string reference,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)]
        RestCheckInRequest? body)
    {
        var result = gateway.CheckIn(reference, body?.Bags ?? []);

        FlightDesignator flightDesignator;
        try
        {
            flightDesignator = gateway.GetBooking(reference).Flight;
        }
        catch
        {
            flightDesignator = new FlightDesignator { Carrier = AirlineCode.Hb, Number = ParseNumber(result.FlightNumber) };
        }

        var response = new CheckInResponse
        {
            BookingRef = result.BookingRef,
            FlightNumber = result.FlightNumber,
            Flight = flightDesignator,
            PassengerName = result.PassengerName,
            Seat = result.Seat,
            Gate = result.Gate,
            ScheduledDepartureUtc = result.ScheduledDepartureUtc,
            BoardingTimeUtc = result.BoardingTimeUtc,
            BoardingSequence = result.BoardingSequence,
            Baggage = result.Bags,
            Warnings = WarningTranslator.Translate(result.Warnings),
        };

        return Ok(response);
    }

    private static int ParseNumber(string flightNumber)
    {
        if (flightNumber.Length >= 3 && int.TryParse(flightNumber[2..], out var n))
        {
            return n;
        }
        return 0;
    }

    /// <summary>
    /// Register ONE piece of baggage at the bag drop after check-in. The body is polymorphic:
    ///   {"type":"checked","weightKg":23,"color":"black","lengthCm":75}  hold luggage
    ///   {"type":"carryOn","weightKg":8,"color":"grey","hasLaptop":true}    cabin luggage
    /// Allowances per bag by cabin: checked 30 kg First/Business, 23 kg Economy;
    /// carry-on 10 kg vs 8 kg. Overweight bags ARE accepted and produce translated
    /// warnings instead of errors. Requires prior check-in; at most one bag of each type.
    /// </summary>
    /// <param name="reference">Booking reference; must be checked in already.</param>
    /// <param name="luggage">Polymorphic bag description with a type discriminator.</param>
    /// <returns>All bags on the booking plus structured warnings.</returns>
    /// <response code="201">Bag accepted; Warnings non-empty when overweight.</response>
    /// <response code="400">Weight out of range or missing discriminator.</response>
    /// <response code="404">Unknown booking.</response>
    /// <response code="409">CHECKIN_REQUIRED, CHECKIN_CLOSED, BAGGAGE_TYPE_LIMIT or FLIGHT_DEPARTED.</response>
    [HttpPost("{reference}/baggage")]
    [ProducesResponseType(typeof(BaggageRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public ActionResult<BaggageRegistrationResponse> RegisterBaggage(string reference, [FromBody] Baggage luggage)
    {
        var reply = gateway.RegisterBaggage(reference, luggage);

        var response = new BaggageRegistrationResponse
        {
            BookingRef = reply.BookingRef,
            Success = reply.Success,
            Baggage = reply.Bags,
            Warnings = WarningTranslator.Translate(reply.Warnings),
        };

        return CreatedAtAction(nameof(GetByRef), new { reference = reply.BookingRef }, response);
    }
}
