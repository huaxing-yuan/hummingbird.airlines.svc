using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Hummingbird.Airlines.Backend.Domain;

/// <summary>Passenger identity attached to a booking.</summary>
[DataContract(Name = "PassengerNameRecord")]
public class Passenger
{
    /// <summary>Given name as printed in the passport.</summary>
    /// <example>John</example>
    [DataMember(Order = 1)]
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name as printed in the passport.</summary>
    /// <example>Doe</example>
    [DataMember(Order = 2)]
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Passport number. Used to look up bookings by passenger.</summary>
    /// <example>P0938211</example>
    [DataMember(Order = 3)]
    [Required]
    public string Passport { get; set; } = string.Empty;
}

/// <summary>A reservation record: passenger, flight, cabin class and check-in results.
/// The store keeps at most 50 bookings (oldest evicted) and nothing is persisted.</summary>
[DataContract(Name = "BookingRecord")]
public class Booking
{
    /// <summary>Six-character booking reference (PNR).</summary>
    /// <example>GZT001</example>
    [DataMember(Order = 1)]
    public string BookingRef { get; set; } = string.Empty;

    /// <summary>Passenger holding the reservation.</summary>
    [DataMember(Order = 2)]
    public Passenger Passenger { get; set; } = new();

    /// <summary>Hummingbird flight number this booking is made for.</summary>
    /// <example>HB102</example>
    [DataMember(Order = 3)]
    public string FlightNumber { get; set; } = string.Empty;

    /// <summary>Cabin class; determines the baggage allowances.</summary>
    [DataMember(Order = 4)]
    public CabinClass CabinClass { get; set; }

    /// <summary>Bags registered at the bag drop so far.</summary>
    [DataMember(Order = 5)]
    public List<Baggage> Bags { get; set; } = [];

    /// <summary>Assigned seat; empty until the booking has been checked in.</summary>
    /// <example>31E</example>
    [DataMember(Order = 6)]
    public string Seat { get; set; } = string.Empty;

    /// <summary>Departure gate, copied from the flight at check-in time.</summary>
    /// <example>C13</example>
    [DataMember(Order = 7)]
    public string Gate { get; set; } = string.Empty;

    /// <summary>Boarding sequence number issued at check-in.</summary>
    /// <example>101</example>
    [DataMember(Order = 8)]
    public int BoardingSequence { get; set; }

    /// <summary>When the booking was checked in; null while still open.</summary>
    /// <example>2026-08-25T18:20:00Z</example>
    [DataMember(Order = 9)]
    public DateTime? CheckedInAtUtc { get; set; }

    /// <summary>Creation time of the booking (UTC). Drives FIFO eviction.</summary>
    /// <example>2026-08-25T17:00:00Z</example>
    [DataMember(Order = 10)]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Structured flight designator (carrier enum + numeric number).</summary>
    [DataMember(Order = 11)]
    public FlightDesignator Flight { get; set; } = new() { Carrier = AirlineCode.Hb, Number = 100 };

    /// <summary>True once the booking has been checked in.</summary>
    [IgnoreDataMember]
    public bool IsCheckedIn => CheckedInAtUtc is not null;
}
