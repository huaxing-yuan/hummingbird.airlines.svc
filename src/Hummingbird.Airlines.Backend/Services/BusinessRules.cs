using Hummingbird.Airlines.Backend.Domain;

namespace Hummingbird.Airlines.Backend.Services;

/// <summary>
/// The airline business rules. All validation lives in the backend systems,
/// never in the middleware, so test clients can exercise each failure mode.
/// </summary>
public static class BusinessRules
{
    /// <summary>Check-in closes this many minutes before departure.</summary>
    public const int CheckInCutoffMinutes = 30;

    /// <summary>Boarding starts this many minutes before departure.</summary>
    public const int BoardingTimeBeforeDepartureMinutes = 40;

    /// <summary>Checked baggage allowance per bag (kg), inclusive.</summary>
    public static double CheckedAllowanceKg(CabinClass cabin) => cabin == CabinClass.Economy ? 23d : 30d;

    /// <summary>Carry-on allowance per bag (kg), inclusive.</summary>
    public static double CarryOnAllowanceKg(CabinClass cabin) => cabin == CabinClass.Economy ? 8d : 10d;
}
