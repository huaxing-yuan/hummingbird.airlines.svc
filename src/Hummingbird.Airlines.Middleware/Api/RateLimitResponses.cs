using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>
/// Writes 429 responses in the shape of the caller:
///   REST  -> application/problem+json with code RATE_LIMITED
///   SOAP  -> a SOAP 1.1 fault envelope with faultstring RATE_LIMITED
/// </summary>
public static class RateLimitResponses
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async ValueTask WriteAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        http.Response.Headers.RetryAfter = "60";

        if (http.Request.Path.StartsWithSegments("/soap"))
        {
            http.Response.ContentType = "text/xml; charset=utf-8";
            const string fault =
                """
                <?xml version="1.0" encoding="utf-8"?>
                <s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
                  <s:Body>
                    <s:Fault>
                      <faultcode>s:Sender</faultcode>
                      <faultstring>RATE_LIMITED</faultstring>
                      <detail><Reason>Too many requests from this address.</Reason></detail>
                    </s:Fault>
                  </s:Body>
                </s:Envelope>
                """;
            await http.Response.WriteAsync(fault, cancellationToken);
            return;
        }

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = "https://hummingbird.airlines/errors/rate-limited",
            Title = "Too many requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Request quota exceeded. Slow down and retry later.",
            Instance = http.Request.Path,
        };
        problem.Extensions["code"] = "RATE_LIMITED";
        problem.Extensions["traceId"] = http.TraceIdentifier;

        http.Response.ContentType = "application/problem+json";
        await http.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions), cancellationToken);
    }
}
