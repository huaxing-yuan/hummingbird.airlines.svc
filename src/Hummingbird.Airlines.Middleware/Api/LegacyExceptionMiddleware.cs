using System.Text.Json;
using Hummingbird.Airlines.Middleware.Soap;
using Hummingbird.Airlines.Middleware.Translation;

namespace Hummingbird.Airlines.Middleware.Api;

/// <summary>
/// Translates legacy failures into RFC 7807 ProblemDetails responses:
///   typed backend fault  -> 4xx with a stable machine-readable code
///   backend timeout      -> 504
///   backend unavailable  -> 502
/// </summary>
public sealed class LegacyExceptionMiddleware(RequestDelegate next, ILogger<LegacyExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (LegacyFaultException fault)
        {
            var (status, title, type) = FaultMapper.Map(fault.Code);
            await WriteProblem(context, status, title, type, fault.Code, fault.Message);
        }
        catch (LegacyTimeoutException timeout)
        {
            await WriteProblem(context, StatusCodes.Status504GatewayTimeout, "Upstream timeout", "legacy-timeout", "LEGACY_TIMEOUT", timeout.Message);
        }
        catch (LegacyUnavailableException unavailable)
        {
            await WriteProblem(context, StatusCodes.Status502BadGateway, "Bad gateway", "legacy-unavailable", "LEGACY_UNAVAILABLE", unavailable.Message);
        }
        catch (NotSupportedException unsupported) when (unsupported.Message.Contains("type discriminator"))
        {
            // Malformed polymorphic JSON (missing "type") must be a client error,
            // not an internal server error.
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Invalid request", "invalid-request", "INVALID_REQUEST",
                "The polymorphic payload must specify a type discriminator.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Internal server error", "internal-error", "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(HttpContext context, int status, string title, string type, string code, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"https://hummingbird.airlines/errors/{type}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
