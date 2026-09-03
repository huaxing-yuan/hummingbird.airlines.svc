using System.Runtime.Serialization;
using System.ServiceModel;
using Hummingbird.Airlines.Backend.Domain;

namespace Hummingbird.Airlines.Backend.Services;

[DataContract(Name = "ServiceFault")]
public class ServiceFault
{
    /// <summary>Stable machine-readable error code, e.g. BOOKING_NOT_FOUND or CHECKIN_CLOSED.</summary>
    /// <example>CHECKIN_CLOSED</example>
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable explanation of the failure.</summary>
    /// <example>Check-in for flight HB900 closed 30 minutes before departure.</example>
    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}

public static class FaultCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string BookingNotFound = "BOOKING_NOT_FOUND";
    public const string FlightNotFound = "FLIGHT_NOT_FOUND";
    public const string FlightDeparted = "FLIGHT_DEPARTED";
    public const string FlightCancelled = "FLIGHT_CANCELLED";
    public const string CheckInRequired = "CHECKIN_REQUIRED";
    public const string CheckInClosed = "CHECKIN_CLOSED";
    public const string AlreadyCheckedIn = "ALREADY_CHECKED_IN";
    public const string BaggageTypeLimit = "BAGGAGE_TYPE_LIMIT";
    public const string InternalError = "INTERNAL_ERROR";
}

internal static class Faults
{
    public static FaultException<ServiceFault> Create(string code, string message) =>
        new(new ServiceFault { Code = code, Message = message }, new FaultReason(message));
}

// ---------------------------------------------------------------------------
// Booking system (reservation legacy system)
// ---------------------------------------------------------------------------

/// <summary>Body of POST /api/v1/bookings: create a reservation for one passenger.</summary>
[DataContract(Name = "CreateBookingRequest")]
public class CreateBookingRequest
{
    /// <summary>Structured flight designator - carrier enum + number (e.g. hb + 102).</summary>
    [DataMember(Order = 1)]
    public FlightDesignator Flight { get; set; } = new() { Carrier = AirlineCode.Hb, Number = 102 };

    /// <summary>Requested cabin class. Determines baggage allowances (economy | business | first).</summary>
    [DataMember(Order = 2)]
    public CabinClass CabinClass { get; set; }

    /// <summary>Passenger identity. All three fields are required.</summary>
    [DataMember(Order = 3)]
    public Passenger Passenger { get; set; } = new();
}

/// <summary>Body of PUT /api/v1/bookings/{ref}: full replacement of the mutable booking fields.
/// Rejected with ALREADY_CHECKED_IN once the booking has been checked in.</summary>
[DataContract(Name = "UpdateBookingRequest")]
public class UpdateBookingRequest
{
    /// <summary>New cabin class (economy | business | first).</summary>
    [DataMember(Order = 1)]
    public CabinClass CabinClass { get; set; }

    /// <summary>New passenger identity (all fields required).</summary>
    [DataMember(Order = 2)]
    public Passenger Passenger { get; set; } = new();
}

/// <summary>Wrapper returned by FindBookings.</summary>
[DataContract(Name = "BookingList")]
public class BookingList
{
    /// <summary>Matching bookings, ordered by creation time.</summary>
    [DataMember(Order = 1)]
    public List<Booking> Items { get; set; } = [];
}

[System.ServiceModel.ServiceContract(Namespace = "http://hummingbird.airlines/booking", Name = "BookingSystem")]
public interface IBookingSystemService
{
    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/booking/CreateBooking", ReplyAction = "http://hummingbird.airlines/booking/CreateBookingResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    Booking CreateBooking(FlightDesignator flight, CabinClass cabinClass, Passenger passenger);

    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/booking/GetBooking", ReplyAction = "http://hummingbird.airlines/booking/GetBookingResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    Booking GetBooking(string bookingRef);

    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/booking/FindBookings", ReplyAction = "http://hummingbird.airlines/booking/FindBookingsResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    BookingList FindBookings(string passport);

    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/booking/UpdateBooking", ReplyAction = "http://hummingbird.airlines/booking/UpdateBookingResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    Booking UpdateBooking(string bookingRef, CabinClass cabinClass, Passenger passenger);

    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/booking/CancelBooking", ReplyAction = "http://hummingbird.airlines/booking/CancelBookingResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    void CancelBooking(string bookingRef);

    /// <summary>
    /// Administrative reset: drops every booking, replays the frozen demo seeds and restarts
    /// all deterministic counters. Global operation (no session isolation); bypasses chaos drills.
    /// </summary>
    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/booking/ResetDemoData", ReplyAction = "http://hummingbird.airlines/booking/ResetDemoDataResponse")]
    BookingList ResetDemoData();
}

// ---------------------------------------------------------------------------
// Flight management (airport flight control - read only)
// ---------------------------------------------------------------------------

/// <summary>Wrapper returned by SearchFlights.</summary>
[DataContract(Name = "FlightList")]
public class FlightList
{
    /// <summary>Flights ordered by scheduled departure.</summary>
    [DataMember(Order = 1)]
    public List<Flight> Items { get; set; } = [];
}

[System.ServiceModel.ServiceContract(Namespace = "http://hummingbird.airlines/flights", Name = "FlightManagement")]
public interface IFlightManagementService
{
    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/flights/SearchFlights", ReplyAction = "http://hummingbird.airlines/flights/SearchFlightsResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    FlightList SearchFlights(string? from, string? to, DateTime? departureDateUtc);

    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/flights/GetFlight", ReplyAction = "http://hummingbird.airlines/flights/GetFlightResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    Flight GetFlight(string flightNumber);
}

// ---------------------------------------------------------------------------
// Luggage management (airport departure control: check-in + baggage drop)
// ---------------------------------------------------------------------------

/// <summary>
/// Soft-failure reply of a baggage registration. Business violations that are NOT fatal
/// (overweight) keep Success=true and are reported as legacy warning strings, translated
/// by the middleware into structured warnings.
/// </summary>
[DataContract(Name = "BaggageRegistrationReply")]
public class BaggageRegistrationReply
{
    /// <summary>Always true unless a fatal fault was raised instead.</summary>
    /// <example>true</example>
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    /// <summary>Booking reference the bags belong to.</summary>
    /// <example>GZT001</example>
    [DataMember(Order = 2)]
    public string BookingRef { get; set; } = string.Empty;

    /// <summary>Pipe-delimited legacy warnings, e.g. W|BAGGAGE_WEIGHT|checked|24.9|23.0</summary>
    [DataMember(Order = 3)]
    public List<string> Warnings { get; set; } = [];

    /// <summary>All bags currently registered on the booking, newest last.</summary>
    [DataMember(Order = 4)]
    public List<Baggage> Bags { get; set; } = [];
}

/// <summary>Boarding pass issued by a successful check-in. Also carries the bags that were accepted together with it.</summary>
[DataContract(Name = "BoardingPass")]
public class CheckInResult
{
    /// <summary>Booking reference that was checked in.</summary>
    /// <example>GZT001</example>
    [DataMember(Order = 1)]
    public string BookingRef { get; set; } = string.Empty;

    /// <summary>Flight number checked in for.</summary>
    /// <example>HB102</example>
    [DataMember(Order = 2)]
    public string FlightNumber { get; set; } = string.Empty;

    /// <summary>Full passenger name as issued on the boarding pass.</summary>
    /// <example>John Doe</example>
    [DataMember(Order = 3)]
    public string PassengerName { get; set; } = string.Empty;

    /// <summary>Assigned seat (row depends on cabin class).</summary>
    /// <example>31E</example>
    [DataMember(Order = 4)]
    public string Seat { get; set; } = string.Empty;

    /// <summary>Departure gate at the origin airport.</summary>
    /// <example>C13</example>
    [DataMember(Order = 5)]
    public string Gate { get; set; } = string.Empty;

    /// <summary>Scheduled departure instant (UTC).</summary>
    /// <example>2026-08-26T16:29:00Z</example>
    [DataMember(Order = 6)]
    public DateTime ScheduledDepartureUtc { get; set; }

    /// <summary>Boarding start instant (UTC), 40 minutes before departure.</summary>
    /// <example>2026-08-26T15:49:00Z</example>
    [DataMember(Order = 7)]
    public DateTime BoardingTimeUtc { get; set; }

    /// <summary>Boarding sequence number.</summary>
    /// <example>101</example>
    [DataMember(Order = 8)]
    public int BoardingSequence { get; set; }

    /// <summary>Bags accepted during this check-in (may include warnings for overweight).</summary>
    [DataMember(Order = 9)]
    public List<Baggage> Bags { get; set; } = [];

    /// <summary>Pipe-delimited legacy warnings for the bags, e.g. W|BAGGAGE_WEIGHT|checked|24.9|23.0</summary>
    [DataMember(Order = 10)]
    public List<string> Warnings { get; set; } = [];
}

[System.ServiceModel.ServiceContract(Namespace = "http://hummingbird.airlines/luggage", Name = "LuggageManagement")]
public interface ILuggageManagementService
{
    /// <summary>
    /// One-shot check-in: validates the booking, assigns seat/gate/sequence and
    /// atomically registers the optional polymorphic bag array.
    /// Request parameters are flat (DCS wrapped style): bookingRef + bags (ArrayOfBaggage).
    /// </summary>
    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/luggage/CheckIn", ReplyAction = "http://hummingbird.airlines/luggage/CheckInResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    CheckInResult CheckIn(string bookingRef, List<Baggage> bags);

    /// <summary>
    /// Register one bag: flat parameters bookingRef + luggage (with xsi:type).
    /// </summary>
    [System.ServiceModel.OperationContract(Action = "http://hummingbird.airlines/luggage/RegisterBaggage", ReplyAction = "http://hummingbird.airlines/luggage/RegisterBaggageResponse")]
    [System.ServiceModel.FaultContract(typeof(ServiceFault))]
    BaggageRegistrationReply RegisterBaggage(string bookingRef, Baggage luggage);
}
