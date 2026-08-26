using System.Globalization;

namespace Hummingbird.Airlines.Middleware.Translation;

/// <summary>A structured warning produced by translating a legacy warning string.</summary>
public sealed class Warning
{
    /// <summary>Stable machine-readable code, e.g. CHECKED_BAGGAGE_OVERWEIGHT.</summary>
    /// <example>CHECKED_BAGGAGE_OVERWEIGHT</example>
    public string Code { get; init; } = string.Empty;

    /// <summary>Warning family; "baggageWeight" today, extensible later.</summary>
    /// <example>baggageWeight</example>
    public string Category { get; init; } = string.Empty;

    /// <summary>Human-readable explanation suitable for end users.</summary>
    /// <example>The checked bag weighs 24.9 kg which exceeds the allowance of 23 kg.</example>
    public string Message { get; init; } = string.Empty;

    /// <summary>Measured weight that triggered the warning, when applicable.</summary>
    /// <example>24.9</example>
    public double? ActualKg { get; init; }

    /// <summary>Allowance that was exceeded, when applicable.</summary>
    /// <example>23</example>
    public double? LimitKg { get; init; }

    /// <summary>Original legacy string for traceability.</summary>
    /// <example>W|BAGGAGE_WEIGHT|checked|24.9|23.0</example>
    public string LegacyMessage { get; init; } = string.Empty;
}

/// <summary>
/// Translates cryptic pipe-delimited legacy warning strings emitted by the
/// airport systems into structured, documented warnings for REST clients.
/// Unknown formats are passed through verbatim instead of being dropped.
///
/// Known legacy format:
///   W|BAGGAGE_WEIGHT|{checked|carryon}|{actualKg}|{limitKg}
/// </summary>
public static class WarningTranslator
{
    public static IReadOnlyList<Warning> Translate(IEnumerable<string>? legacyWarnings)
    {
        if (legacyWarnings is null)
        {
            return Array.Empty<Warning>();
        }

        return legacyWarnings.Select(Translate).ToArray();
    }

    private static Warning Translate(string raw)
    {
        var parts = raw.Split('|');

        if (parts.Length == 5
            && parts[0].Equals("W", StringComparison.Ordinal)
            && parts[1].Equals("BAGGAGE_WEIGHT", StringComparison.OrdinalIgnoreCase))
        {
            var type = parts[2].Equals("checked", StringComparison.OrdinalIgnoreCase) ? "checked" : "carryOn";
            var actualKg = ParseDouble(parts[3]);
            var limitKg = ParseDouble(parts[4]);

        return new Warning
        {
            Code = type == "checked" ? "CHECKED_BAGGAGE_OVERWEIGHT" : "CARRY_ON_BAGGAGE_OVERWEIGHT",
            Category = "baggageWeight",
            Message = $"The {type} bag weighs {Format(actualKg)} kg which exceeds the allowance of {Format(limitKg)} kg.",
            ActualKg = actualKg,
            LimitKg = limitKg,
            LegacyMessage = raw,
        };
        }

        return new Warning
        {
            Code = "UNKNOWN_LEGACY_WARNING",
            Category = "unknown",
            Message = raw,
            ActualKg = null,
            LimitKg = null,
            LegacyMessage = raw,
        };
    }

    private static double? ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static string Format(double? value) => value?.ToString("0.#", CultureInfo.InvariantCulture) ?? "?";
}
