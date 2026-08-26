using System.Globalization;
using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Backend.Storage;

namespace Hummingbird.Airlines.Backend.Services;

/// <summary>
/// Airport departure control system: check-in and baggage drop.
/// </summary>
public class LuggageManagementService : ILuggageManagementService
{
    private readonly FlightScheduleStore _flights;
    private readonly BookingStore _bookings;

    /// <summary>Live check-ins start above the seeded demo value (PRS205 = 101).</summary>
    private static int _boardingSequence = 200;

    /// <summary>Deterministic sequential bag tag source (process-wide, monotonic).</summary>
    private static long _tagCounter;

    public LuggageManagementService(FlightScheduleStore flights, BookingStore bookings)
    {
        _flights = flights;
        _bookings = bookings;
    }

    public CheckInResult CheckIn(CheckInRequest request)
    {
        Chaos.Apply();

        if (request is null || string.IsNullOrWhiteSpace(request.BookingRef))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "bookingRef is required");
        }

        var booking = _bookings.Update(request.BookingRef, _ => { })
            ?? throw Faults.Create(FaultCodes.BookingNotFound, $"No booking found for reference '{request.BookingRef}'");

        if (booking.IsCheckedIn)
        {
            throw Faults.Create(FaultCodes.AlreadyCheckedIn, $"Booking {booking.BookingRef} has already been checked in");
        }

        var flight = _flights.GetByNumber(booking.FlightNumber)!;
        var remaining = flight.ScheduledDepartureUtc - DateTime.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            throw Faults.Create(FaultCodes.FlightDeparted, $"Flight {flight.FlightNumber} has already departed");
        }

        if (remaining < TimeSpan.FromMinutes(BusinessRules.CheckInCutoffMinutes))
        {
            throw Faults.Create(
                FaultCodes.CheckInClosed,
                $"Check-in for flight {flight.FlightNumber} closed {BusinessRules.CheckInCutoffMinutes} minutes before departure ({Math.Floor(remaining.TotalMinutes)} min remaining)");
        }

        // ---- validate polymorphic bag array atomically (at most one of each kind) ----
        var incoming = request.Bags ?? [];
        if (incoming.Count > 2)
        {
            throw Faults.Create(FaultCodes.BaggageTypeLimit, "At most one checked bag and one carry-on per passenger");
        }

        var hasCheckedIncoming = incoming.OfType<CheckedBaggage>().Count() > 1;
        var hasCarryIncoming = incoming.OfType<CarryOnBaggage>().Count() > 1;
        if (hasCheckedIncoming || hasCarryIncoming)
        {
            throw Faults.Create(FaultCodes.BaggageTypeLimit, "At most one bag of each type per passenger");
        }

        // existing bags (should be empty since not checked in yet, but enforce anyway)
        if (booking.Bags.OfType<CheckedBaggage>().Any() && incoming.OfType<CheckedBaggage>().Any())
        {
            throw Faults.Create(FaultCodes.BaggageTypeLimit, "Passenger already has a checked bag");
        }
        if (booking.Bags.OfType<CarryOnBaggage>().Any() && incoming.OfType<CarryOnBaggage>().Any())
        {
            throw Faults.Create(FaultCodes.BaggageTypeLimit, "Passenger already has a carry-on bag");
        }

        foreach (var bag in incoming)
        {
            if (bag.WeightKg <= 0 || bag.WeightKg > 100)
            {
                throw Faults.Create(FaultCodes.InvalidRequest, "luggage weight must be between 0 and 100 kg");
            }
        }

        var warnings = new List<string>();
        foreach (var bag in incoming)
        {
            var allowance = bag switch
            {
                CheckedBaggage => BusinessRules.CheckedAllowanceKg(booking.CabinClass),
                CarryOnBaggage => BusinessRules.CarryOnAllowanceKg(booking.CabinClass),
                _ => double.MaxValue,
            };
            if (bag.WeightKg > allowance)
            {
                var type = bag is CheckedBaggage ? "checked" : "carryon";
                warnings.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "W|BAGGAGE_WEIGHT|{0}|{1:F1}|{2:F1}",
                    type, bag.WeightKg, allowance));
            }
            bag.TagId = string.IsNullOrWhiteSpace(bag.TagId)
                ? $"HB-{Interlocked.Increment(ref _tagCounter):D8}"
                : bag.TagId;
        }

        booking.Seat = AssignSeat(booking.BookingRef, booking.CabinClass);
        booking.Gate = flight.Gate ?? _flights.GetRawGate(flight.FlightNumber) ?? "TBD";
        booking.BoardingSequence = Interlocked.Increment(ref _boardingSequence);
        booking.CheckedInAtUtc = DateTime.UtcNow;
        foreach (var bag in incoming)
        {
            booking.Bags.Add(bag);
        }

        return new CheckInResult
        {
            BookingRef = booking.BookingRef,
            FlightNumber = flight.FlightNumber,
            PassengerName = $"{booking.Passenger.FirstName} {booking.Passenger.LastName}",
            Seat = booking.Seat,
            Gate = booking.Gate,
            ScheduledDepartureUtc = flight.ScheduledDepartureUtc,
            BoardingTimeUtc = flight.ScheduledDepartureUtc.AddMinutes(-BusinessRules.BoardingTimeBeforeDepartureMinutes),
            BoardingSequence = booking.BoardingSequence,
            Bags = [.. booking.Bags],
            Warnings = warnings,
        };
    }

    public BaggageRegistrationReply RegisterBaggage(BaggageRegistrationRequest request)
    {
        Chaos.Apply();

        if (request?.Luggage is null || string.IsNullOrWhiteSpace(request.BookingRef))
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "bookingRef and luggage are required");
        }

        if (request.Luggage.WeightKg <= 0 || request.Luggage.WeightKg > 100)
        {
            throw Faults.Create(FaultCodes.InvalidRequest, "luggage weight must be between 0 and 100 kg");
        }

        var booking = _bookings.Update(request.BookingRef, _ => { })
            ?? throw Faults.Create(FaultCodes.BookingNotFound, $"No booking found for reference '{request.BookingRef}'");

        if (!booking.IsCheckedIn)
        {
            throw Faults.Create(FaultCodes.CheckInRequired, $"Booking {booking.BookingRef} must be checked in before baggage can be registered");
        }

        var flight = _flights.GetByNumber(booking.FlightNumber)!;
        var remaining = flight.ScheduledDepartureUtc - DateTime.UtcNow;

        if (remaining < TimeSpan.FromMinutes(BusinessRules.CheckInCutoffMinutes))
        {
            throw Faults.Create(FaultCodes.CheckInClosed, $"Baggage drop for flight {flight.FlightNumber} is closed");
        }

        // one bag per type
        var isChecked = request.Luggage is CheckedBaggage;
        if (isChecked && booking.Bags.OfType<CheckedBaggage>().Any())
        {
            throw Faults.Create(FaultCodes.BaggageTypeLimit, "Passenger already has a checked bag (max one per type)");
        }
        if (!isChecked && booking.Bags.OfType<CarryOnBaggage>().Any())
        {
            throw Faults.Create(FaultCodes.BaggageTypeLimit, "Passenger already has a carry-on bag (max one per type)");
        }

        var warnings = new List<string>();
        var allowance = request.Luggage switch
        {
            CheckedBaggage => BusinessRules.CheckedAllowanceKg(booking.CabinClass),
            CarryOnBaggage => BusinessRules.CarryOnAllowanceKg(booking.CabinClass),
            _ => double.MaxValue,
        };

        if (request.Luggage.WeightKg > allowance)
        {
            var type = request.Luggage is CheckedBaggage ? "checked" : "carryon";
            warnings.Add(string.Format(
                CultureInfo.InvariantCulture,
                "W|BAGGAGE_WEIGHT|{0}|{1:F1}|{2:F1}",
                type, request.Luggage.WeightKg, allowance));
        }

        request.Luggage.TagId = string.IsNullOrWhiteSpace(request.Luggage.TagId)
            ? $"HB-{Interlocked.Increment(ref _tagCounter):D8}"
            : request.Luggage.TagId;

        _bookings.Update(request.BookingRef, b => b.Bags.Add(request.Luggage));

        return new BaggageRegistrationReply
        {
            Success = true,
            BookingRef = booking.BookingRef,
            Warnings = warnings,
            Bags = [.. booking.Bags],
        };
    }

    private static string AssignSeat(string bookingRef, CabinClass cabin) => cabin switch
    {
        CabinClass.First => SeatFromHash(bookingRef, rows: (1, 3), letters: "ACDF"),
        CabinClass.Business => SeatFromHash(bookingRef, rows: (5, 9), letters: "ABCDEF"),
        _ => SeatFromHash(bookingRef, rows: (12, 45), letters: "ABCDEFK"),
    };

    private static string SeatFromHash(string seed, (int Min, int Max) rows, string letters)
    {
        var hash = Math.Abs(StringHash(seed));
        var row = rows.Min + hash % (rows.Max - rows.Min + 1);
        return $"{row}{letters[hash % letters.Length]}";
    }

    private static int StringHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
}
