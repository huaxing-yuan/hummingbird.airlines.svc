using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Backend.Storage;

namespace Hummingbird.Airlines.Backend.Services;

/// <summary>
/// Read-only airport flight control system.
/// </summary>
public class FlightManagementService : IFlightManagementService
{
    private readonly FlightScheduleStore _flights;

    public FlightManagementService(FlightScheduleStore flights) => _flights = flights;

    public FlightList SearchFlights(string? from, string? to, DateTime? departureDateUtc)
    {
        Chaos.Apply();

        return new FlightList { Items = [.. _flights.Search(from, to, departureDateUtc)] };
    }

    public Flight GetFlight(string flightNumber)
    {
        Chaos.Apply();

        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "flightNumber is required");
        }

        return _flights.GetByNumber(flightNumber)
            ?? throw Faults.Create(FaultCodes.FlightNotFound, $"No flight found with number '{flightNumber}'");
    }
}
