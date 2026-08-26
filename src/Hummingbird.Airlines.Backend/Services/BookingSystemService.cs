using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Backend.Storage;

namespace Hummingbird.Airlines.Backend.Services;

public class BookingSystemService : IBookingSystemService
{
    private readonly FlightScheduleStore _flights;
    private readonly BookingStore _bookings;

    public BookingSystemService(FlightScheduleStore flights, BookingStore bookings)
    {
        _flights = flights;
        _bookings = bookings;
    }

    public Booking CreateBooking(CreateBookingRequest request)
    {
        Chaos.Apply();

        if (request?.Flight is null)
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "Flight designator is required");
        }

        if (request.Passenger is null
            || string.IsNullOrWhiteSpace(request.Passenger.FirstName)
            || string.IsNullOrWhiteSpace(request.Passenger.LastName)
            || string.IsNullOrWhiteSpace(request.Passenger.Passport))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "Passenger FirstName, LastName and Passport are required");
        }

        var flightNumber = $"{request.Flight.Carrier.ToString().ToUpperInvariant()}{request.Flight.Number}";
        var flight = _flights.GetByNumber(flightNumber);
        if (flight is null)
        {
            throw Faults.Create(FaultCodes.FlightNotFound, $"No flight found with number '{flightNumber}'");
        }

        if (flight.Status == FlightState.Cancelled)
        {
            throw Faults.Create(FaultCodes.FlightCancelled, $"Flight {flight.FlightNumber} has been cancelled");
        }

        if (flight.ScheduledDepartureUtc <= DateTime.UtcNow)
        {
            throw Faults.Create(FaultCodes.FlightDeparted, $"Flight {flight.FlightNumber} has already departed");
        }

        var booking = new Booking
        {
            BookingRef = _bookings.GenerateRef(),
            Passenger = request.Passenger,
            FlightNumber = flight.FlightNumber,
            Flight = new FlightDesignator { Carrier = flight.Designator.Carrier, Number = flight.Designator.Number },
            CabinClass = request.CabinClass,
            CreatedAtUtc = DateTime.UtcNow,
        };

        return Enrich(_bookings.Add(booking));
    }

    public Booking GetBooking(string bookingRef)
    {
        Chaos.Apply();

        if (string.IsNullOrWhiteSpace(bookingRef))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "bookingRef is required");
        }

        var booking = _bookings.Get(bookingRef)
            ?? throw Faults.Create(FaultCodes.BookingNotFound, $"No booking found for reference '{bookingRef}'");
        return Enrich(booking);
    }

    public BookingList FindBookings(string passport)
    {
        Chaos.Apply();

        if (string.IsNullOrWhiteSpace(passport))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "passport is required");
        }

        var items = _bookings.FindByPassport(passport).Select(Enrich).ToList();
        return new BookingList { Items = [.. items] };
    }

    public Booking UpdateBooking(string bookingRef, UpdateBookingRequest request)
    {
        Chaos.Apply();

        if (request is null)
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "Request body is required");
        }

        if (request.Passenger is null
            || string.IsNullOrWhiteSpace(request.Passenger.FirstName)
            || string.IsNullOrWhiteSpace(request.Passenger.LastName)
            || string.IsNullOrWhiteSpace(request.Passenger.Passport))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "Passenger FirstName, LastName and Passport are required");
        }

        var updated = _bookings.Update(bookingRef, booking =>
        {
            if (booking.IsCheckedIn)
            {
                throw Faults.Create(FaultCodes.AlreadyCheckedIn, $"Booking {booking.BookingRef} is already checked in and can no longer be modified");
            }

            booking.CabinClass = request.CabinClass;
            booking.Passenger = request.Passenger;
        });

        return Enrich(updated
            ?? throw Faults.Create(FaultCodes.BookingNotFound, $"No booking found for reference '{bookingRef}'"));
    }

    public void CancelBooking(string bookingRef)
    {
        Chaos.Apply();

        if (!_bookings.Delete(bookingRef))
        {
            throw Faults.Create(FaultCodes.BookingNotFound, $"No booking found for reference '{bookingRef}'");
        }
    }

    private Booking Enrich(Booking booking)
    {
        var flight = _flights.GetByNumber(booking.FlightNumber);
        if (flight is not null)
        {
            booking.Flight = new FlightDesignator { Carrier = flight.Designator.Carrier, Number = flight.Designator.Number };
        }
        return booking;
    }
}
