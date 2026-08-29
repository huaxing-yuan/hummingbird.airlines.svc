using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Hummingbird.Airlines.Backend.Domain;

/// <summary>An airport in the Hummingbird network.</summary>
[DataContract(Name = "Airport")]
public class Airport
{
    /// <summary>IATA airport code.</summary>
    /// <example>CDG</example>
    [DataMember(Order = 1)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Full airport name.</summary>
    /// <example>Paris Charles de Gaulle Airport</example>
    [DataMember(Order = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>City served by the airport.</summary>
    /// <example>Paris</example>
    [DataMember(Order = 3)]
    public string City { get; set; } = string.Empty;

    /// <summary>Country of the airport.</summary>
    /// <example>France</example>
    [DataMember(Order = 4)]
    public string Country { get; set; } = string.Empty;

    /// <summary>Home terminal of the airline at this airport.</summary>
    /// <example>2E</example>
    [DataMember(Order = 5)]
    public string Terminal { get; set; } = string.Empty;
}

/// <summary>Structured flight designator: carrier code plus numeric flight number.</summary>
[DataContract(Name = "FlightDesignator")]
public class FlightDesignator
{
    /// <summary>Operating carrier.</summary>
    /// <example>hb</example>
    [DataMember(Order = 1)]
    public AirlineCode Carrier { get; set; }

    /// <summary>Numeric part of the flight number (without carrier prefix).</summary>
    /// <example>900</example>
    [DataMember(Order = 2)]
    public int Number { get; set; }

    /// <summary>Combined textual flight number, e.g. HB900 (derived, for convenience).</summary>
    /// <example>HB900</example>
    public string DisplayNumber => $"{Carrier.ToString().ToUpperInvariant()}{Number:D3}";

    public override string ToString() => DisplayNumber;
}

/// <summary>A scheduled flight. Departure instants are computed from the current clock,
/// so the values returned are always consistent with "now".</summary>
[DataContract(Name = "FlightInformation")]
public class Flight
{
    /// <summary>Hummingbird flight number as a single string (e.g. HB900).</summary>
    /// <example>HB900</example>
    [DataMember(Order = 1)]
    public string FlightNumber { get; set; } = string.Empty;

    /// <summary>Structured designator: airline enum + numeric number.</summary>
    [DataMember(Order = 2)]
    public FlightDesignator Designator { get; set; } = new() { Carrier = AirlineCode.Hb, Number = 900 };

    /// <summary>Departure airport (includes terminal).</summary>
    [DataMember(Order = 3)]
    public Airport From { get; set; } = new();

    /// <summary>Arrival airport (includes terminal).</summary>
    [DataMember(Order = 4)]
    public Airport To { get; set; } = new();

    /// <summary>Departure terminal at the origin airport.</summary>
    /// <example>2E</example>
    [DataMember(Order = 5)]
    public string DepartureTerminal { get; set; } = string.Empty;

    /// <summary>Arrival terminal at the destination airport.</summary>
    /// <example>3</example>
    [DataMember(Order = 6)]
    public string ArrivalTerminal { get; set; } = string.Empty;

    /// <summary>Planned departure instant (UTC).</summary>
    /// <example>2026-08-26T16:29:00Z</example>
    [DataMember(Order = 7)]
    public DateTime ScheduledDepartureUtc { get; set; }

    /// <summary>Planned arrival instant (UTC), derived from the route duration.</summary>
    /// <example>2026-08-27T03:00:00Z</example>
    [DataMember(Order = 8)]
    public DateTime ScheduledArrivalUtc { get; set; }

    /// <summary>Aircraft type operating the flight.</summary>
    /// <example>Airbus A350-900</example>
    [DataMember(Order = 9)]
    public string AircraftModel { get; set; } = string.Empty;

    /// <summary>
    /// Boarding gate at the departure terminal. Only populated while the flight is in
    /// CheckInOpen or Boarding state; otherwise null (code generators see a nullable string).
    /// </summary>
    /// <example>B12</example>
    [DataMember(Order = 10, EmitDefaultValue = false, IsRequired = false)]
    public string? Gate { get; set; }

    /// <summary>True when the airline cancelled the flight; bookings cannot be made for it.</summary>
    /// <example>false</example>
    [DataMember(Order = 11)]
    public bool IsCancelled { get; set; }

    /// <summary>Operational status derived from the time remaining before departure.</summary>
    [DataMember(Order = 12)]
    public FlightState Status { get; set; }
}

/// <summary>
/// A piece of luggage registered at the bag drop. The JSON body is polymorphic:
/// use <c>{"type":"checked",...}</c> for hold luggage or <c>{"type":"carryOn",...}</c>
/// for cabin baggage. Allowances per bag: checked 30 kg (First/Business) or 23 kg
/// (Economy); carry-on 10 kg or 8 kg respectively. Each passenger may hold at most
/// one bag of each kind.
/// </summary>
[KnownType(typeof(CheckedBaggage))]
[KnownType(typeof(CarryOnBaggage))]
[JsonConverter(typeof(BaggageJsonConverter))]
[DataContract(Name = "Baggage")]
public abstract class Baggage
{
    /// <summary>Weight of the bag in kilograms; must be greater than 0 and at most 100.</summary>
    /// <example>24.5</example>
    [DataMember(Order = 1)]
    public double WeightKg { get; set; }

    /// <summary>Optional free-text colour used to identify the bag.</summary>
    /// <example>red</example>
    [DataMember(Order = 2)]
    public string Color { get; set; } = string.Empty;

    /// <summary>Bag tag identifier. Assigned by the system when omitted.</summary>
    /// <example>HB-00000001</example>
    [DataMember(Order = 3)]
    public string TagId { get; set; } = string.Empty;
}

/// <summary>Hold luggage transported in the aircraft belly. Has dimensions and fragility.</summary>
[DataContract(Name = "CheckedBaggage")]
public class CheckedBaggage : Baggage
{
    /// <summary>Length in centimetres.</summary>
    /// <example>75</example>
    [DataMember(Order = 4)]
    public int LengthCm { get; set; } = 75;

    /// <summary>Width in centimetres.</summary>
    /// <example>48</example>
    [DataMember(Order = 5)]
    public int WidthCm { get; set; } = 48;

    /// <summary>Height in centimetres.</summary>
    /// <example>28</example>
    [DataMember(Order = 6)]
    public int HeightCm { get; set; } = 28;

    /// <summary>Whether the bag contains fragile items.</summary>
    /// <example>false</example>
    [DataMember(Order = 7)]
    public bool Fragile { get; set; }
}

/// <summary>Cabin baggage taken into the passenger cabin. Has laptop/under-seat hints.</summary>
[DataContract(Name = "CarryOnBaggage")]
public class CarryOnBaggage : Baggage
{
    /// <summary>Whether a laptop or large electronic device is inside.</summary>
    /// <example>true</example>
    [DataMember(Order = 4)]
    public bool HasLaptop { get; set; }

    /// <summary>Whether the bag fits under the seat in front.</summary>
    /// <example>false</example>
    [DataMember(Order = 5)]
    public bool FitsUnderSeat { get; set; } = true;
}
