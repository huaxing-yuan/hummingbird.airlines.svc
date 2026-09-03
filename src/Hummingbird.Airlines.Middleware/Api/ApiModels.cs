using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Backend.Services;
using Hummingbird.Airlines.Middleware.Translation;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>Result of the one-shot check-in (may carry a polymorphic bag array).</summary>
public sealed class CheckInResponse
{
    /// <summary>Booking reference that was checked in.</summary>
    /// <example>GZT001</example>
    public string BookingRef { get; init; } = string.Empty;

    /// <summary>Flight number checked in for.</summary>
    /// <example>HB102</example>
    public string FlightNumber { get; init; } = string.Empty;

    /// <summary>Structured flight designator (carrier + number).</summary>
    public FlightDesignator Flight { get; init; } = new() { Carrier = AirlineCode.Hb, Number = 100 };

    /// <summary>Full passenger name as issued on the boarding pass.</summary>
    /// <example>John Doe</example>
    public string PassengerName { get; init; } = string.Empty;

    /// <summary>Assigned seat (row depends on cabin class).</summary>
    /// <example>31E</example>
    public string Seat { get; init; } = string.Empty;

    /// <summary>Departure gate at the origin terminal.</summary>
    /// <example>C13</example>
    public string Gate { get; init; } = string.Empty;

    /// <summary>Scheduled departure instant (UTC).</summary>
    /// <example>2026-08-26T16:29:00Z</example>
    public DateTime ScheduledDepartureUtc { get; init; }

    /// <summary>Boarding start instant (UTC), 40 minutes before departure.</summary>
    /// <example>2026-08-26T15:49:00Z</example>
    public DateTime BoardingTimeUtc { get; init; }

    /// <summary>Boarding sequence number.</summary>
    /// <example>101</example>
    public int BoardingSequence { get; init; }

    /// <summary>Bags accepted during this check-in (polymorphic array items).</summary>
    public IReadOnlyList<Baggage> Baggage { get; init; } = [];

    /// <summary>Structured warnings translated from legacy codes (overweight etc.).</summary>
    public IReadOnlyList<Warning> Warnings { get; init; } = [];
}

/// <summary>Result of a baggage-drop registration: the accepted bags plus translated warnings.</summary>
public sealed class BaggageRegistrationResponse
{
    /// <summary>Booking reference the bags were registered for.</summary>
    /// <example>GZT001</example>
    public string BookingRef { get; init; } = string.Empty;

    /// <summary>Always true when this object is returned; hard failures use ProblemDetails instead.</summary>
    /// <example>true</example>
    public bool Success { get; init; }

    /// <summary>All bags currently on the booking, each tagged with its discriminator type.</summary>
    public IReadOnlyList<Baggage> Baggage { get; init; } = [];

    /// <summary>Structured warnings translated from legacy codes. Empty when every bag was within allowance.</summary>
    public IReadOnlyList<Warning> Warnings { get; init; } = [];
}

/// <summary>Envelope of GET /api/v1/bookings?passport=...</summary>
public sealed class BookingListResponse
{
    /// <summary>Bookings of the passenger, oldest first.</summary>
    public IReadOnlyList<Booking> Items { get; init; } = [];

    /// <summary>Convenience count equal to Items.Count.</summary>
    /// <example>2</example>
    public int Count { get; init; }
}

/// <summary>Result of POST /api/v1/admin/reset: all in-memory demo data restored to fresh state.</summary>
public sealed class ResetResult
{
    /// <summary>Always true when the reset completed.</summary>
    /// <example>true</example>
    public bool Reset { get; init; }

    /// <summary>UTC instant the reset was applied.</summary>
    /// <example>2026-09-03T12:00:00Z</example>
    public DateTime ResetAtUtc { get; init; }

    /// <summary>Booking references restored by reseeding (the frozen demo set).</summary>
    /// <example>["GZT001", "QWX452", "LMN789", "PRS205", "TRV310"]</example>
    public IReadOnlyList<string> DemoBookings { get; init; } = [];
}
