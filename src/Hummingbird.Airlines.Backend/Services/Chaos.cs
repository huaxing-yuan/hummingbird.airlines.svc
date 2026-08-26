using System.ServiceModel;
using Microsoft.AspNetCore.Http;

namespace Hummingbird.Airlines.Backend.Services;

/// <summary>
/// Failure injection for test scenarios. Every backend operation calls
/// <see cref="Apply"/> first; the directive can be supplied either as a plain
/// HTTP header when the SOAP endpoint is called directly, or as a SOAP header
/// with the same name when the call is relayed by the middleware.
///
/// Supported directives:
///   fault          -> typed INTERNAL_ERROR soap fault
///   unavailable    -> unhandled crash (becomes an untyped soap fault)
///   timeout        -> sleep 15 s (exceeds the middleware client timeout)
///   timeout=N      -> sleep N seconds
/// </summary>
public static class Chaos
{
    public const string HeaderName = "X-HB-Simulate";
    public const string HeaderNamespace = "http://hummingbird.airlines/test";

    private static readonly IHttpContextAccessor Accessor = new HttpContextAccessor();

    public static void Apply()
    {
        var directive = ReadDirective();
        if (string.IsNullOrWhiteSpace(directive))
        {
            return;
        }

        var value = directive.Trim();

        if (value.Equals("fault", StringComparison.OrdinalIgnoreCase))
        {
            throw Faults.Create(FaultCodes.InternalError, "Simulated backend failure");
        }

        if (value.Equals("unavailable", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Simulated legacy system crash");
        }

        if (value.StartsWith("timeout", StringComparison.OrdinalIgnoreCase))
        {
            var seconds = 15;
            var separator = value.IndexOf('=');
            if (separator >= 0 && int.TryParse(value[(separator + 1)..], out var parsed))
            {
                seconds = parsed;
            }

            Thread.Sleep(TimeSpan.FromSeconds(Math.Max(0, seconds)));
        }
    }

    private static string? ReadDirective()
    {
        var operationContext = OperationContext.Current;
        if (operationContext?.IncomingMessageHeaders is not null)
        {
            try
            {
                var index = operationContext.IncomingMessageHeaders.FindHeader(HeaderName, HeaderNamespace);
                if (index >= 0)
                {
                    var forwarded = operationContext.IncomingMessageHeaders.GetHeader<string>(index);
                    if (!string.IsNullOrWhiteSpace(forwarded))
                    {
                        return forwarded;
                    }
                }
            }
            catch
            {
                // Ignore malformed headers.
            }
        }

        if (Accessor.HttpContext?.Request.Headers.TryGetValue(HeaderName, out var values) == true)
        {
            return values.ToString();
        }

        return null;
    }
}
