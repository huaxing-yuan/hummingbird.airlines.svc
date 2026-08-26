using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hummingbird.Airlines.Backend.Services;
using Hummingbird.Airlines.Backend.Storage;
using Hummingbird.Airlines.Middleware.Api;
using Hummingbird.Airlines.Middleware.Soap;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using SoapCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// Abuse protection: hard transport-level caps.
// -----------------------------------------------------------------------
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 400;
    options.Limits.MaxRequestBodySize =
        builder.Configuration.GetValue<long?>("Protection:MaxRequestBodySizeBytes") ?? 262_144;
});

// The service runs behind cloud proxies (Azure App Service front end) whose networks
// are unknown/unstable: trust any X-Forwarded-For chain. Per-IP quotas are keyed on
// the forwarded client address; spoofing it cannot exceed the server-wide ceiling.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = RateLimitResponses.WriteAsync;

    var protection = builder.Configuration.GetSection("Protection");
    var globalRpm = protection.GetValue("GlobalRequestsPerMinute", 3000);
    var apiRpm = protection.GetValue("ApiRequestsPerMinute", 120);
    var soapRpm = protection.GetValue("SoapRequestsPerMinute", 240);
    var otherRpm = protection.GetValue("OtherRequestsPerMinute", 120);
    var maxConcurrentPerIp = protection.GetValue("MaxConcurrentPerIp", 16);
    var internalToken = builder.Configuration["Internal:SharedSecret"] ?? string.Empty;

    bool IsInternalCall(HttpContext http) =>
        !string.IsNullOrEmpty(internalToken)
        && string.Equals(http.Request.Headers["X-HB-Internal-Token"], internalToken, StringComparison.Ordinal);

    var ceiling = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
        RateLimitPartition.GetFixedWindowLimiter("server-wide-ceiling",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = globalRpm, Window = TimeSpan.FromMinutes(1) }));

    var perCaller = PartitionedRateLimiter.Create<HttpContext, string>(http =>
    {
        if (IsInternalCall(http))
        {
            return RateLimitPartition.GetNoLimiter("internal");
        }

        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var (partition, permitPerMinute) = http.Request.Path.StartsWithSegments("/soap")
            ? ($"soap:{ip}", soapRpm)
            : http.Request.Path.StartsWithSegments("/api")
                ? ($"api:{ip}", apiRpm)
                : ($"other:{ip}", otherRpm);

        return RateLimitPartition.GetFixedWindowLimiter(partition,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitPerMinute, Window = TimeSpan.FromMinutes(1) });
    });

    var parallelCalls = PartitionedRateLimiter.Create<HttpContext, string>(http =>
    {
        if (IsInternalCall(http))
        {
            return RateLimitPartition.GetNoLimiter("internal-concurrency");
        }

        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetConcurrencyLimiter($"concurrency:{ip}",
            _ => new ConcurrencyLimiterOptions { PermitLimit = maxConcurrentPerIp, QueueLimit = 0 });
    });

    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(ceiling, perCaller, parallelCalls);
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AddHttpContextAccessor();

// Required by SoapCore >= 1.2 so FaultException<T> is serialized into a proper
// SOAP fault response instead of crashing while building the error message.
builder.Services.AddSingleton<SoapCore.Extensibility.IFaultExceptionTransformer>(
    new SoapCore.DefaultFaultExceptionTransformer<SoapCore.CustomMessage>());

builder.Services.AddSingleton<FlightScheduleStore>();
builder.Services.AddSingleton<BookingStore>();
builder.Services.AddSingleton<BookingSystemService>();
builder.Services.AddSingleton<FlightManagementService>();
builder.Services.AddSingleton<LuggageManagementService>();
builder.Services.AddSingleton<SoapGateway>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Hummingbird Airlines Gateway API",
        Version = "v1",
        Description = """
            REST gateway (EAI middleware) in front of three simulated SOAP 1.1 legacy systems:
              * BookingSystem   - reservations CRUD
              * FlightManagement - read-only flight control
              * LuggageManagement - check-in and baggage drop
            All business validation lives in the backends; the middleware only translates
            protocol, status codes and warnings.
            """,
    });

    // Emit oneOf/discriminator schemas for the STJ polymorphic baggage hierarchy.
    options.UseOneOfForPolymorphism();

    // Feed <summary>/<example> XML comments into description/example metadata so
    // code generators (Aethrix object tree) can surface hints to the user.
    var xmlFiles = new[]
    {
        $"{typeof(Program).Assembly.GetName().Name}.xml",
        "Hummingbird.Airlines.Backend.xml",
    };
    foreach (var xmlFile in xmlFiles)
    {
        var path = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(path))
        {
            options.IncludeXmlComments(path, includeControllerXmlComments: true);
        }
    }
});

var app = builder.Build();

// Real client address (behind the Azure front end) before anything reads it.
app.UseForwardedHeaders();

// Reject abusive traffic before it reaches any endpoint, middleware or backend.
((IApplicationBuilder)app).UseRateLimiter();

app.UseMiddleware<LegacyExceptionMiddleware>();

// Default document + static assets for the service directory page.
app.UseDefaultFiles();
app.UseStaticFiles();

// Interactive REST documentation (SOAP side publishes ?wsdl per endpoint).
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hummingbird Gateway v1"));

app.MapControllers();

// Current protection settings, rendered on the default page.
app.MapGet("/api/v1/_protection", (IConfiguration config) =>
{
    var p = config.GetSection("Protection");
    return Results.Json(new
    {
        globalRequestsPerMinute = p.GetValue("GlobalRequestsPerMinute", 3000),
        apiRequestsPerMinute = p.GetValue("ApiRequestsPerMinute", 120),
        soapRequestsPerMinute = p.GetValue("SoapRequestsPerMinute", 240),
        otherRequestsPerMinute = p.GetValue("OtherRequestsPerMinute", 120),
        maxConcurrentPerIp = p.GetValue("MaxConcurrentPerIp", 16),
        maxRequestBodySizeBytes = p.GetValue("MaxRequestBodySizeBytes", 262_144L),
    });
});

// The three simulated legacy endpoints (SOAP 1.1 over POST, WSDL on GET ?wsdl).
((IApplicationBuilder)app).UseSoapEndpoint<BookingSystemService>(options =>
{
    options.Path = "/soap/booking";
    options.SoapSerializer = SoapSerializer.DataContractSerializer;
    options.HttpGetEnabled = true;
    options.HttpsGetEnabled = true;
    options.IndentXml = true;
});
((IApplicationBuilder)app).UseSoapEndpoint<FlightManagementService>(options =>
{
    options.Path = "/soap/flights";
    options.SoapSerializer = SoapSerializer.DataContractSerializer;
    options.HttpGetEnabled = true;
    options.HttpsGetEnabled = true;
    options.IndentXml = true;
});
((IApplicationBuilder)app).UseSoapEndpoint<LuggageManagementService>(options =>
{
    options.Path = "/soap/luggage";
    options.SoapSerializer = SoapSerializer.DataContractSerializer;
    options.HttpGetEnabled = true;
    options.HttpsGetEnabled = true;
    options.IndentXml = true;
});

// Extra loopback binding used by the middleware to reach the legacy endpoints
// through real HTTP round-trips inside the same process/container.
// Note: touching app.Urls disables Kestrel's implicit defaults (including the
// platform-injected ASPNETCORE_HTTP_PORTS), so resolve the public port ourselves:
//   Linux App Service front end -> 8080 (set ASPNETCORE_HTTP_PORTS=8080),
//   local development           -> launchSettings / fallback 5000.
var publicPort =
    Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")
    ?? Environment.GetEnvironmentVariable("PORT")
    ?? "5000";

var internalUrl = app.Configuration["InternalUrl"] ?? "http://127.0.0.1:5150";

foreach (var portEntry in publicPort.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
{
    app.Urls.Add($"http://+:{portEntry}");
}
app.Urls.Add(internalUrl);

app.Run();


