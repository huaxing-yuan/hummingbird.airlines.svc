using System.Runtime.Serialization;

namespace Hummingbird.Airlines.Backend.Domain;

/// <summary>IATA-style carrier codes. The schedule only contains Hummingbird (Hb) flights;
/// other members exist so negative tests can use an unsupported carrier and still get a valid enum value.</summary>
[DataContract(Name = "AirlineCode")]
public enum AirlineCode
{
    /// <summary>Hummingbird Airlines (the only carrier present in the schedule).</summary>
    /// <example>hb</example>
    [EnumMember]
    Hb = 0,

    /// <summary>Air France.</summary>
    [EnumMember]
    Af = 1,

    /// <summary>British Airways.</summary>
    [EnumMember]
    Ba = 2,

    /// <summary>Air China.</summary>
    [EnumMember]
    Ca = 3,

    /// <summary>Lufthansa.</summary>
    [EnumMember]
    Lh = 4,

    /// <summary>Emirates.</summary>
    [EnumMember]
    Ek = 5,
}

/// <summary>Cabin class of a booking. Determines the baggage allowances.</summary>
[DataContract]
public enum CabinClass
{
    /// <summary>Economy: checked 23 kg / carry-on 8 kg per bag.</summary>
    [EnumMember]
    Economy = 0,

    /// <summary>Business: checked 30 kg / carry-on 10 kg per bag.</summary>
    [EnumMember]
    Business = 1,

    /// <summary>First: checked 30 kg / carry-on 10 kg per bag.</summary>
    [EnumMember]
    First = 2,
}

/// <summary>
/// Operational state of a flight, derived from the current time on every read:
/// Scheduled &gt; 60 min before departure, CheckInOpen within 60 min, Boarding within 25 min,
/// Departed afterwards; Cancelled is a fixed flag.
/// </summary>
[DataContract(Name = "FlightStatus")]
public enum FlightState
{
    /// <summary>Departure more than 60 minutes ahead.</summary>
    [EnumMember]
    Scheduled = 0,

    /// <summary>Check-in counter open: departure between 25 and 60 minutes away.</summary>
    [EnumMember]
    CheckInOpen = 1,

    /// <summary>Gate open: departure less than 25 minutes away.</summary>
    [EnumMember]
    Boarding = 2,

    /// <summary>The aircraft has left.</summary>
    [EnumMember]
    Departed = 3,

    /// <summary>Flight was cancelled by the airline.</summary>
    [EnumMember]
    Cancelled = 4,
}
