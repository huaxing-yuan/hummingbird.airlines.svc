using System.ServiceModel;
using System.ServiceModel.Channels;
using Hummingbird.Airlines.Backend.Domain;
using Hummingbird.Airlines.Backend.Services;

namespace Hummingbird.Airlines.Middleware.Soap;

/// <summary>A typed fault returned by a legacy backend system.</summary>
public sealed class LegacyFaultException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>The legacy system did not answer in time.</summary>
public sealed class LegacyTimeoutException(string endpoint, TimeSpan timeout)
    : Exception($"Legacy endpoint '{endpoint}' did not respond within {timeout.TotalSeconds:F0} s")
{
    public string Endpoint { get; } = endpoint;
}

/// <summary>The legacy system is unreachable or crashed.</summary>
public sealed class LegacyUnavailableException(string message) : Exception(message);

/// <summary>
/// Typed SOAP 1.1 (BasicHttpBinding) client towards the three legacy backend
/// endpoints. The middleware NEVER calls backend services in-process: every
/// operation goes through a real HTTP round-trip on the loopback interface,
/// exactly like an enterprise service bus talking to mainframe systems.
/// </summary>
public sealed class SoapGateway
{
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<SoapGateway> _logger;
    private readonly ChannelFactory<IBookingSystemService> _bookingFactory;
    private readonly ChannelFactory<IFlightManagementService> _flightFactory;
    private readonly ChannelFactory<ILuggageManagementService> _luggageFactory;

    public SoapGateway(IConfiguration configuration, IHttpContextAccessor http, ILogger<SoapGateway> logger)
    {
        _http = http;
        _logger = logger;

        var baseUrl = (configuration["Legacy:BaseUrl"] ?? "http://127.0.0.1:5150").TrimEnd('/');
        var timeout = TimeSpan.FromSeconds(configuration.GetValue("Legacy:TimeoutSeconds", 10));

        BasicHttpBinding Binding()
        {
            var binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 8 * 1024 * 1024,
                SendTimeout = timeout,
                ReceiveTimeout = timeout,
                OpenTimeout = timeout,
                CloseTimeout = timeout,
            };
            binding.ReaderQuotas.MaxStringContentLength = 2 * 1024 * 1024;
            binding.ReaderQuotas.MaxArrayLength = 64 * 1024;
            return binding;
        }

        _bookingFactory = new ChannelFactory<IBookingSystemService>(Binding(), new EndpointAddress($"{baseUrl}/soap/booking"));
        _flightFactory = new ChannelFactory<IFlightManagementService>(Binding(), new EndpointAddress($"{baseUrl}/soap/flights"));
        _luggageFactory = new ChannelFactory<ILuggageManagementService>(Binding(), new EndpointAddress($"{baseUrl}/soap/luggage"));
    }

    // ----- Booking system -----

    public Booking CreateBooking(CreateBookingRequest request) =>
        Exec(_bookingFactory, "booking", channel => channel.CreateBooking(request));

    public Booking GetBooking(string bookingRef) =>
        Exec(_bookingFactory, "booking", channel => channel.GetBooking(bookingRef));

    public List<Booking> FindBookings(string passport) =>
        Exec(_bookingFactory, "booking", channel => channel.FindBookings(passport).Items);

    public Booking UpdateBooking(string bookingRef, UpdateBookingRequest request) =>
        Exec(_bookingFactory, "booking", channel => channel.UpdateBooking(bookingRef, request));

    public void CancelBooking(string bookingRef) =>
        Exec(_bookingFactory, "booking", channel =>
        {
            channel.CancelBooking(bookingRef);
            return true;
        });

    // ----- Flight management -----

    public List<Flight> SearchFlights(string? from, string? to, DateTime? departureDateUtc) =>
        Exec(_flightFactory, "flights", channel => channel.SearchFlights(from, to, departureDateUtc).Items);

    public Flight GetFlight(string flightNumber) =>
        Exec(_flightFactory, "flights", channel => channel.GetFlight(flightNumber));

    // ----- Luggage management -----

    public CheckInResult CheckIn(CheckInRequest request) =>
        Exec(_luggageFactory, "luggage", channel => channel.CheckIn(request));

    public BaggageRegistrationReply RegisterBaggage(BaggageRegistrationRequest request) =>
        Exec(_luggageFactory, "luggage", channel => channel.RegisterBaggage(request));

    // ----- Plumbing -----

    private T Exec<TClient, T>(ChannelFactory<TClient> factory, string endpointName, Func<TClient, T> operation)
        where TClient : class
    {
        var channel = factory.CreateChannel();
        try
        {
            using var scope = new OperationContextScope((IContextChannel)channel);
            ForwardSimulationHeader();

            var result = operation(channel);
            ((IClientChannel)channel).Close();
            return result;
        }
        catch (FaultException<ServiceFault> fault)
        {
            ((IClientChannel)channel).Abort();
            var code = fault.Detail?.Code ?? FaultCodes.InternalError;
            var message = fault.Detail?.Message ?? fault.Message;
            _logger.LogWarning("Legacy {Endpoint} rejected the call with {Code}: {Message}", endpointName, code, message);
            throw new LegacyFaultException(code, message);
        }
        catch (FaultException fault)
        {
            // Untyped fault -> the legacy process crashed before serializing a typed fault.
            ((IClientChannel)channel).Abort();
            _logger.LogError(fault, "Legacy {Endpoint} returned an untyped fault", endpointName);
            throw new LegacyUnavailableException($"Legacy endpoint '{endpointName}' failed: {fault.Message}");
        }
        catch (TimeoutException timeout)
        {
            ((IClientChannel)channel).Abort();
            _logger.LogError(timeout, "Legacy {Endpoint} timed out", endpointName);
            throw new LegacyTimeoutException(endpointName, factory.Endpoint.Binding.SendTimeout);
        }
        catch (CommunicationException communication)
        {
            ((IClientChannel)channel).Abort();
            _logger.LogError(communication, "Legacy {Endpoint} is unreachable", endpointName);
            throw new LegacyUnavailableException($"Legacy endpoint '{endpointName}' is unreachable: {communication.Message}");
        }
        catch
        {
            try { ((IClientChannel)channel).Abort(); } catch { /* ignore */ }
            throw;
        }
    }

    /// <summary>
    /// Relays the X-HB-Simulate chaos header from the incoming REST request to
    /// the outgoing SOAP call as an HTTP header, so failure injection works no
    /// matter which of the four endpoints the test tool targets.
    /// </summary>
    private void ForwardSimulationHeader()
    {
        if (_http.HttpContext?.Request.Headers.TryGetValue("X-HB-Simulate", out var values) != true)
        {
            return;
        }

        var directive = values.ToString();
        if (string.IsNullOrWhiteSpace(directive) || OperationContext.Current is null)
        {
            return;
        }

        if (OperationContext.Current.OutgoingMessageProperties.TryGetValue(HttpRequestMessageProperty.Name, out var existing)
            && existing is HttpRequestMessageProperty existingProperty)
        {
            existingProperty.Headers["X-HB-Simulate"] = directive;
            return;
        }

        var httpProperty = new HttpRequestMessageProperty
        {
            Headers = { { "X-HB-Simulate", directive } }
        };
        OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpProperty;
    }
}
