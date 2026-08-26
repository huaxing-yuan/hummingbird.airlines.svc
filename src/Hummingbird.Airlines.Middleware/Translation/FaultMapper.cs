namespace Hummingbird.Airlines.Middleware.Translation;

public static class FaultMapper
{
    public static (int Status, string Title, string Type) Map(string code) => code switch
    {
        "INVALID_REQUEST" => (StatusCodes.Status400BadRequest, "Invalid request", "invalid-request"),
        "BOOKING_NOT_FOUND" => (StatusCodes.Status404NotFound, "Booking not found", "booking-not-found"),
        "FLIGHT_NOT_FOUND" => (StatusCodes.Status404NotFound, "Flight not found", "flight-not-found"),
        "FLIGHT_DEPARTED" => (StatusCodes.Status409Conflict, "Flight already departed", "flight-departed"),
        "FLIGHT_CANCELLED" => (StatusCodes.Status409Conflict, "Flight cancelled", "flight-cancelled"),
        "CHECKIN_REQUIRED" => (StatusCodes.Status409Conflict, "Check-in required", "checkin-required"),
        "CHECKIN_CLOSED" => (StatusCodes.Status409Conflict, "Check-in closed", "checkin-closed"),
        "ALREADY_CHECKED_IN" => (StatusCodes.Status409Conflict, "Already checked in", "already-checked-in"),
        "BAGGAGE_TYPE_LIMIT" => (StatusCodes.Status409Conflict, "Baggage type limit exceeded", "baggage-type-limit"),
        _ => (StatusCodes.Status502BadGateway, "Legacy system failure", "legacy-failure"),
    };
}
