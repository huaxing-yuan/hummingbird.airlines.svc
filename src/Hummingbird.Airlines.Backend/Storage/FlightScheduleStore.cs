using Hummingbird.Airlines.Backend.Domain;

namespace Hummingbird.Airlines.Backend.Storage;

/// <summary>
/// Virtual flight schedule shared by all backend systems.
///
/// Nothing is frozen at startup: flights are stored as <see cref="FlightTemplate"/>s and
/// materialised on every read. Departure instants are computed from the CURRENT clock -
///   * hot flights:   now + fixed minute offsets (test scenarios never age),
///   * line flights:  today's date + day offset + planned time of day (rolls over daily),
/// and the operational status (scheduled / check-in open / boarding / departed /
/// cancelled) is derived from the remaining time at read time.
/// </summary>
public sealed class FlightScheduleStore
{
    private sealed record FlightTemplate(
        string FlightNumber,
        AirlineCode Carrier,
        int DesignatorNumber,
        string FromCode,
        string ToCode,
        string Gate,
        string AircraftModel,
        bool IsCancelled,
        int? HotOffsetMinutes,
        int DayOffset,
        int PlannedHour,
        int PlannedMinute);

    private const int HotFlightCount = 8;

    /// <summary>Airports referenced by the schedule (IATA -> details).</summary>
    public static readonly IReadOnlyDictionary<string, Airport> Airports = new Dictionary<string, Airport>(StringComparer.OrdinalIgnoreCase)
    {
        ["PEK"] = new Airport { Code = "PEK", Name = "Beijing Capital International Airport", City = "Beijing", Country = "China", Terminal = "3" },
        ["CDG"] = new Airport { Code = "CDG", Name = "Paris Charles de Gaulle Airport", City = "Paris", Country = "France", Terminal = "2E" },
        ["JFK"] = new Airport { Code = "JFK", Name = "John F. Kennedy International Airport", City = "New York", Country = "United States", Terminal = "1" },
        ["LHR"] = new Airport { Code = "LHR", Name = "London Heathrow Airport", City = "London", Country = "United Kingdom", Terminal = "5" },
        ["FRA"] = new Airport { Code = "FRA", Name = "Frankfurt Airport", City = "Frankfurt", Country = "Germany", Terminal = "1" },
        ["DXB"] = new Airport { Code = "DXB", Name = "Dubai International Airport", City = "Dubai", Country = "United Arab Emirates", Terminal = "3" },
        ["HND"] = new Airport { Code = "HND", Name = "Tokyo Haneda Airport", City = "Tokyo", Country = "Japan", Terminal = "2" },
        ["SIN"] = new Airport { Code = "SIN", Name = "Singapore Changi Airport", City = "Singapore", Country = "Singapore", Terminal = "1" },
    };

    private static readonly (string From, string To, double Hours)[] Routes =
    [
        ("PEK", "CDG", 10.5), ("CDG", "PEK", 11.0),
        ("CDG", "JFK", 8.5), ("JFK", "CDG", 7.25),
        ("LHR", "JFK", 7.75), ("JFK", "LHR", 6.75),
        ("FRA", "PEK", 9.5), ("PEK", "FRA", 10.25),
        ("HND", "SIN", 7.0), ("SIN", "HND", 6.75),
        ("DXB", "JFK", 14.0), ("JFK", "DXB", 12.5),
        ("CDG", "DXB", 6.5), ("DXB", "CDG", 7.0),
        ("PEK", "HND", 3.5), ("HND", "PEK", 4.0),
        ("SIN", "CDG", 13.0), ("CDG", "SIN", 12.75),
    ];

    private static readonly string[] Aircraft =
    [
        "Airbus A350-900",
        "Boeing 787-9",
        "Airbus A330-300",
        "Boeing 777-300ER",
    ];

    private readonly List<FlightTemplate> _templates = [];
    private readonly Dictionary<string, FlightTemplate> _byNumber = new(StringComparer.OrdinalIgnoreCase);

    public FlightScheduleStore() => BuildTemplates();

    public int Count => _templates.Count;

    public IReadOnlyList<Flight> Search(string? from, string? to, DateTime? departureDateUtc)
    {
        IEnumerable<FlightTemplate> query = _templates;

        if (!string.IsNullOrWhiteSpace(from))
        {
            query = query.Where(t => t.FromCode.Equals(from.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            query = query.Where(t => t.ToCode.Equals(to.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var result = query.Select(Materialize).ToList();

        if (departureDateUtc is { } date)
        {
            var utcDate = date.Date;
            result.RemoveAll(f => f.ScheduledDepartureUtc.Date != utcDate);
        }

        return result.OrderBy(f => f.ScheduledDepartureUtc).ToList();
    }

    public Flight? GetByNumber(string flightNumber) =>
        _byNumber.TryGetValue(flightNumber, out var template) ? Materialize(template) : null;

    /// <summary>Raw gate string from the template - always populated even when Flight.Gate is null.</summary>
    public string? GetRawGate(string flightNumber) =>
        _byNumber.TryGetValue(flightNumber, out var template) ? template.Gate : null;

    /// <summary>Flight number of the i-th "hot" flight (minutes-before-departure scenarios).</summary>
    public string HotFlightNumber(int index) => _templates[^HotFlightCount..][index].FlightNumber;

    /// <summary>First flight number of a route departing tomorrow or later (demo bookings).
    /// Anchored to day offsets so the departure always lies in the future.</summary>
    public string? FirstFutureFlight(string from, string to)
    {
        return _templates
            .Where(t => t.FromCode == from && t.ToCode == to && t.DayOffset >= 1 && !t.IsCancelled)
            .OrderBy(t => t.DayOffset).ThenBy(t => t.PlannedHour)
            .Select(t => t.FlightNumber)
            .FirstOrDefault();
    }

    private void BuildTemplates()
    {
        var counter = 100;

        for (var day = 0; day <= 1; day++)
        {
            for (var routeIndex = 0; routeIndex < Routes.Length; routeIndex++)
            {
                var (from, to, _) = Routes[routeIndex];
                for (var slot = 0; slot < 2; slot++)
                {
                    var hour = 5 + ((routeIndex * 3 + slot * 11) % 16);
                    var minute = (routeIndex * 17 + slot * 29) % 60;

                    AddTemplate(new FlightTemplate(
                        FlightNumber: $"HB{counter}",
                        Carrier: AirlineCode.Hb,
                        DesignatorNumber: counter,
                        FromCode: from,
                        ToCode: to,
                        Gate: $"{(char)('A' + counter % 4)}{1 + counter % 18}",
                        AircraftModel: Aircraft[counter % Aircraft.Length],
                        IsCancelled: false,
                        HotOffsetMinutes: null,
                        DayOffset: day,
                        PlannedHour: hour,
                        PlannedMinute: minute));
                    counter++;
                }
            }
        }

        // One line flight is always cancelled so FLIGHT_CANCELLED stays exercisable.
        var victimIndex = Math.Min(13, _templates.Count - 1 - HotFlightCount);
        var victim = _templates[victimIndex];
        _templates[victimIndex] = victim with { IsCancelled = true };
        _byNumber[victim.FlightNumber] = _templates[victimIndex];

        // Hot departures: fixed minute offsets FROM THE CURRENT TIME, re-applied on every
        // read so the cutoff / boarding scenarios work no matter how long the app ran.
        int[] hotOffsetsMinutes = [29, 31, 45, 55, 90, 150, 240, 360];
        (string From, string To)[] hotRoutes =
        [
            ("PEK", "CDG"), ("CDG", "PEK"), ("CDG", "JFK"), ("JFK", "CDG"),
            ("PEK", "HND"), ("HND", "PEK"), ("CDG", "DXB"), ("DXB", "CDG"),
        ];
        for (var i = 0; i < HotFlightCount; i++)
        {
            AddTemplate(new FlightTemplate(
                FlightNumber: $"HB{900 + i}",
                Carrier: AirlineCode.Hb,
                DesignatorNumber: 900 + i,
                FromCode: hotRoutes[i].From,
                ToCode: hotRoutes[i].To,
                Gate: $"B{10 + i}",
                AircraftModel: Aircraft[i % Aircraft.Length],
                IsCancelled: false,
                HotOffsetMinutes: hotOffsetsMinutes[i],
                DayOffset: 0,
                PlannedHour: 0,
                PlannedMinute: 0));
        }
    }

    private void AddTemplate(FlightTemplate template)
    {
        _templates.Add(template);
        _byNumber[template.FlightNumber] = template;
    }

    private Flight Materialize(FlightTemplate template)
    {
        var now = DateTime.UtcNow;
        var hours = RouteHours(template.FromCode, template.ToCode);

        var departure = template.HotOffsetMinutes is { } offsetMinutes
            ? now.AddMinutes(offsetMinutes)
            : now.Date
                .AddDays(template.DayOffset)
                .AddHours(template.PlannedHour)
                .AddMinutes(template.PlannedMinute);

        var status = DeriveStatus(template.IsCancelled, departure - now);

        return new Flight
        {
            FlightNumber = template.FlightNumber,
            Designator = new FlightDesignator { Carrier = template.Carrier, Number = template.DesignatorNumber },
            From = Airports[template.FromCode],
            To = Airports[template.ToCode],
            DepartureTerminal = Airports[template.FromCode].Terminal,
            ArrivalTerminal = Airports[template.ToCode].Terminal,
            ScheduledDepartureUtc = departure,
            ScheduledArrivalUtc = departure.AddHours(hours),
            AircraftModel = template.AircraftModel,
            Gate = status is FlightState.CheckInOpen or FlightState.Boarding ? template.Gate : null,
            IsCancelled = template.IsCancelled,
            Status = status,
        };

        static FlightState DeriveStatus(bool cancelled, TimeSpan remaining)
        {
            if (cancelled)
            {
                return FlightState.Cancelled;
            }

            if (remaining <= TimeSpan.Zero)
            {
                return FlightState.Departed;
            }

            if (remaining <= TimeSpan.FromMinutes(25))
            {
                return FlightState.Boarding;
            }

            if (remaining <= TimeSpan.FromMinutes(60))
            {
                return FlightState.CheckInOpen;
            }

            return FlightState.Scheduled;
        }
    }

    private static double RouteHours(string from, string to) =>
        Routes.First(r => r.From == from && r.To == to).Hours;
}

