using Hummingbird.Airlines.Backend.Domain;

namespace Hummingbird.Airlines.Backend.Storage;

/// <summary>
/// In-memory booking store with a hard capacity of 50 records.
/// When the capacity is exceeded, the oldest booking is evicted.
/// Nothing is persisted: restarting the service resets everything.
/// </summary>
public sealed class BookingStore
{
    public const int Capacity = 50;

    private readonly object _lock = new();
    private readonly List<Booking> _bookings = new();
    private readonly FlightScheduleStore _flights;
    private long _refCounter;

    public BookingStore(FlightScheduleStore flights)
    {
        _flights = flights;
        SeedDemoBookings(flights);
    }

    public int Count { get { lock (_lock) return _bookings.Count; } }

    public Booking Add(Booking booking)
    {
        lock (_lock)
        {
            _bookings.Add(booking);

            while (_bookings.Count > Capacity)
            {
                var oldest = _bookings.OrderBy(b => b.CreatedAtUtc).First();
                _bookings.Remove(oldest);
            }

            return booking;
        }
    }

    public Booking? Get(string bookingRef)
    {
        lock (_lock)
        {
            return _bookings.FirstOrDefault(b => b.BookingRef.Equals(bookingRef, StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyList<Booking> FindByPassport(string passport)
    {
        lock (_lock)
        {
            return _bookings
                .Where(b => b.Passenger.Passport.Equals(passport.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.CreatedAtUtc)
                .ToList();
        }
    }

    public Booking? Update(string bookingRef, Action<Booking> update)
    {
        lock (_lock)
        {
            var booking = Get(bookingRef);
            if (booking is null)
            {
                return null;
            }

            update(booking);
            return booking;
        }
    }

    public bool Delete(string bookingRef)
    {
        lock (_lock)
        {
            var booking = Get(bookingRef);
            return booking is not null && _bookings.Remove(booking);
        }
    }

    /// <summary>
    /// Deterministic booking reference for auto-created bookings: T00001, T00002, ...
    /// The counter restarts with the process, so the same call sequence always
    /// yields the same references. (Demo bookings use fixed refs and never collide:
    /// they contain no T-prefix.)
    /// </summary>
    public string GenerateRef() => $"T{Interlocked.Increment(ref _refCounter):D5}";

    public IReadOnlyList<Booking> GetAll()
    {
        lock (_lock)
        {
            return _bookings.OrderBy(b => b.CreatedAtUtc).ToList();
        }
    }

    /// <summary>
    /// Restore fresh state: drop every booking, restart the deterministic ref counter,
    /// and replay the frozen demo seeds with fresh timestamps.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _bookings.Clear();
            _refCounter = 0;
            SeedDemoBookings(_flights);
        }
    }

    private void SeedDemoBookings(FlightScheduleStore flights)
    {
        // Deliberately seeded oldest-first so FIFO eviction behaves predictably.
        var now = DateTime.UtcNow;

        void AddSeed(string bookingRef, Passenger passenger, string? flightNumber, CabinClass cabin)
        {
            if (flightNumber is null)
            {
                return;
            }

            _bookings.Add(new Booking
            {
                BookingRef = bookingRef,
                Passenger = passenger,
                FlightNumber = flightNumber,
                Flight = ParseDesignator(flightNumber),
                CabinClass = cabin,
                CreatedAtUtc = now.AddMinutes(-30),
            });
        }

        static FlightDesignator ParseDesignator(string flightNumber)
        {
            if (flightNumber.Length >= 3 && Enum.TryParse<AirlineCode>(flightNumber[..2], true, out var carrier)
                && int.TryParse(flightNumber[2..], out var number))
            {
                return new FlightDesignator { Carrier = carrier, Number = number };
            }
            return new FlightDesignator { Carrier = AirlineCode.Hb, Number = 100 };
        }

        // 1. Far-future economy booking: happy-path check-in.
        AddSeed("GZT001", new Passenger { FirstName = "John", LastName = "Doe", Passport = "P0938211" },
            flights.FirstFutureFlight("PEK", "CDG"), CabinClass.Economy);

        // 2. Business booking, check-in open (~45 min before departure):
        //    overweight checked bag up to 30 kg can be demonstrated here.
        AddSeed("QWX452", new Passenger { FirstName = "Alice", LastName = "Martin", Passport = "P4429087" },
            flights.HotFlightNumber(2), CabinClass.Business);

        // 3. Economy booking inside the check-in cutoff (~29 min): triggers CHECKIN_CLOSED.
        AddSeed("LMN789", new Passenger { FirstName = "Bob", LastName = "Chen", Passport = "P1122334" },
            flights.HotFlightNumber(0), CabinClass.Economy);

        // 4. First-class booking, already checked in: triggers ALREADY_CHECKED_IN
        //    and is ready for baggage-drop scenarios.
        var preCheckedInFlight = flights.GetByNumber(flights.HotFlightNumber(3));
        if (preCheckedInFlight is not null)
        {
            _bookings.Add(new Booking
            {
                BookingRef = "PRS205",
                Passenger = new Passenger { FirstName = "Carol", LastName = "Dupont", Passport = "P5512099" },
                FlightNumber = preCheckedInFlight.FlightNumber,
                Flight = preCheckedInFlight.Designator,
                CabinClass = CabinClass.First,
                Seat = "2A",
                Gate = preCheckedInFlight.Gate ?? flights.GetRawGate(preCheckedInFlight.FlightNumber) ?? "B12",
                BoardingSequence = 101,
                CheckedInAtUtc = now.AddMinutes(-10),
                CreatedAtUtc = now.AddMinutes(-25),
            });
        }

        // 5. Generic booking for update/delete tests.
        AddSeed("TRV310", new Passenger { FirstName = "David", LastName = "Wang", Passport = "P7701266" },
            flights.FirstFutureFlight("JFK", "CDG"), CabinClass.Economy);
    }
}

