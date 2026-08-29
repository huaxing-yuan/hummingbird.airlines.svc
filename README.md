# hummingbird.airlines.svc

![Hummingbird Airlines](Hummingbird-Airlines.png)

An **API testing playground** built for [Aethrix](https://hummingbird-alm.com): one ASP.NET Core
web app (net10.0) hosting a REST middleware plus three simulated SOAP 1.1 "legacy enterprise"
backends. Designed so test scenarios can deliberately trigger every failure mode: hard faults,
soft warnings, timeouts, unavailable backends and capacity eviction.

## Architecture

```
Test client (Aethrix)
  | REST/JSON + OpenAPI                    | raw SOAP 1.1 + WSDL
  v                                        v
+--------------------------------------------------------------------------+
| one Azure Web App - ASP.NET Core, .NET 10                                |
|                                                                          |
|  Middleware (REST)   /api/v1/**      protocol translation ONLY:          |
|    |                                     no business validation          |
|    | real SOAP 1.1 HTTP round-trips                                      |
|    +--> /soap/booking   BookingSystemService    (reservations)           |
|    +--> /soap/flights   FlightManagementService (flight control, RO)     |
|    +--> /soap/luggage   LuggageManagementService (check-in + bag drop)   |
+--------------------------------------------------------------------------+
```

* `src/Hummingbird.Airlines.Backend` - service contracts (`[ServiceContract]`), the three
  legacy services, shared in-memory stores and all business rules.
* `src/Hummingbird.Airlines.Middleware` - web host. Mounts the SOAP endpoints with SoapCore
  (WSDL at `?wsdl`), exposes the REST gateway with Swagger UI, and translates backend
  failures into RFC 7807 ProblemDetails.
* The middleware talks to the backends through the official WCF client stack
  (`ChannelFactory<T>` + `BasicHttpBinding`) over loopback HTTP - never in-process - so a
  failing backend is indistinguishable from a real remote outage.

## Run locally

```powershell
dotnet run --project src/Hummingbird.Airlines.Middleware
# public: http://localhost:5000  (Swagger UI at /swagger)
# internal SOAP loopback: http://127.0.0.1:5150 (configured in appsettings.json)

./tools/smoke.ps1            # end-to-end smoke suite against http://localhost:5000
```

## Deployment

Live instance: **https://hummingbird-airline.azurewebsites.net**

Deploy to your own Azure App Service (Linux, .NET 10) with the included tooling:

```powershell
./tools/deploy-azure.ps1 -ResourceGroup <your-rg> -WebAppName <your-app>
./tools/deploy-azure.ps1 -ResourceGroup <your-rg> -WebAppName <your-app> -VerifyOnly
```

The script publishes (`dotnet publish`), zip-deploys and probes the endpoints.

Notes:
* The Linux front end forwards to container port 8080; the app reads it from the
  `ASPNETCORE_HTTP_PORTS=8080` app setting (locally it falls back to 5000).

## Endpoints

| Endpoint | Kind | Description |
|---|---|---|
| `/` | HTML | default document: service directory + live test data |
| `/api/v1/flights` | REST | flight search (`?from=&to=&date=`), read-only |
| `/api/v1/flights/{nr}` | REST | single flight with live status |
| `/api/v1/bookings` | REST | booking CRUD |
| `/api/v1/bookings/{ref}/check-in` | REST | one-shot check-in with optional polymorphic bag array (`bags: Baggage[]`) |
| `/api/v1/bookings/{ref}/baggage` | REST | polymorphic single-bag drop (also one-per-type) |
| `/api/v1/_protection` | REST | current abuse-protection settings (rendered on the landing page) |
| `/swagger` | REST | Swagger UI for the whole REST surface |
| `/soap/booking?wsdl` | SOAP 1.1 | reservations system |
| `/soap/flights?wsdl` | SOAP 1.1 | airport flight control (read-only) |
| `/soap/luggage?wsdl` | SOAP 1.1 | departure control: check-in + bag drop |

REST is documented through **Swagger** (`/swagger`, machine-readable at
`/swagger/v1/swagger.json`); every SOAP endpoint publishes its **WSDL** at `?wsdl`
(importable by Aethrix / SoapUI / Postman). Both are linked from the default page.

### Default page

`/` serves a static directory page (`wwwroot/index.html`) that lists the four services with
their Swagger/WSDL links, the frozen test data below, business-rule and warning-translation
quick references, chaos-header examples, the current protection limits, and **live state**
for the hot flights and demo bookings (refreshed every 20 s via the public REST API).

## Abuse protection

All four endpoints sit behind the same protection pipeline so nobody can use this public
test service for bulk attacks or load testing. Configured under `Protection` in
`appsettings.json`:

| Scope | Default limit | Rejection |
|---|---|---|
| per IP - REST `/api/v1/*` | 120 requests / min | 429 |
| per IP - SOAP `/soap/*` | 240 requests / min | 429 |
| per IP - pages & assets | 120 requests / min | 429 |
| per IP - concurrency | 16 parallel requests | 429 (no queuing) |
| server wide (all callers) | 3000 requests / min | 429 |
| request body size | 256 KB (Kestrel) | 413 |

* Fixed-window quotas; responses carry `Retry-After: 60`.
* The 429 body matches the caller's dialect - REST gets ProblemDetails
  (`code: RATE_LIMITED`), SOAP gets a SOAP 1.1 fault envelope with
  `faultstring RATE_LIMITED`.
* Middleware-to-backend loopback traffic (no `X-Forwarded-For` on a loopback socket) is
  exempt, otherwise the gateway would throttle itself.
* Client address comes from `X-Forwarded-For` because Azure App Service front ends proxy to
  localhost. The proxy chain is trusted unconditionally (unknown cloud networks), so per-IP
  keys can be spoofed - the server-wide ceiling and concurrency caps still bound total load.
  Honest trade-off for a free-tier test target.

## Business rules (validated in the BACKENDS only)

| Rule | Behaviour |
|---|---|
| check-in < 30 min before departure | SOAP fault `CHECKIN_CLOSED` -> middleware **409** |
| checked bag > allowance (First/Business 30 kg, Economy 23 kg, inclusive) | bag accepted, legacy warning string -> middleware **2xx** + structured warning |
| carry-on > allowance (First/Business 10 kg, Economy 8 kg, inclusive) | same soft-warning behaviour |
| more than one bag of the same type per passenger | fault `BAGGAGE_TYPE_LIMIT` -> **409** |
| baggage before check-in | fault `CHECKIN_REQUIRED` -> **409** |
| unknown booking / flight | faults -> **404** |
| cancelled or departed flight | faults -> **409** |
| bookings table capacity | 50 records FIFO; oldest evicted |

### Translation layers (the interesting part)

1. **Faults**: backend `FaultException<ServiceFault>` carries a stable code
   (`BOOKING_NOT_FOUND`, `CHECKIN_CLOSED`, ...). The middleware maps codes to HTTP statuses
   and returns `application/problem+json`:

   ```json
   {
     "type": "https://hummingbird.airlines/errors/checkin-closed",
     "title": "Check-in closed",
     "status": 409,
     "detail": "Check-in for flight HB900 closed 30 minutes before departure (28 min remaining)",
     "code": "CHECKIN_CLOSED",
     "traceId": "..."
   }
   ```

   Transport failures become **504 LEGACY_TIMEOUT** / **502 LEGACY_UNAVAILABLE**.

2. **Warnings**: overweight baggage is *not* an error. The backend replies `Success=true`
   with a cryptic pipe-delimited legacy string, which the middleware parses into structure:

   ```
   legacy in :  W|BAGGAGE_WEIGHT|checked|24.9|23.0
   JSON out:    { "code": "CHECKED_BAGGAGE_OVERWEIGHT", "category": "baggageWeight",
                  "message": "The checked bag weighs 24.9 kg which exceeds the allowance of 23 kg.",
                  "actualKg": 24.9, "limitKg": 23, "legacyMessage": "W|BAGGAGE_WEIGHT|checked|24.9|23.0" }
   ```

3. **Polymorphism**: baggage bodies are discriminated unions -
   `{"type":"checked","weightKg":23}` or `{"type":"carryOn","weightKg":8}` - implemented with
   `System.Text.Json` polymorphism on the REST side and `[KnownType]`/`xsi:type` on the SOAP
   side. The REST reader is tolerant: it also accepts the Newtonsoft-style
   `"$type":"...CheckedBaggage, ..."` discriminator emitted by generated clients (e.g. Aethrix).
   OpenAPI exposes the union as `oneOf` plus a `discriminator { propertyName: "type" }`.

### Failure injection (chaos header)

All four endpoints accept `X-HB-Simulate`. The relayed value also works through the
middleware (forwarded as an outgoing HTTP header on the SOAP call):

| Value | Effect via middleware |
|---|---|
| `timeout=12` | backend sleeps 12 s -> client timeout -> **504** |
| `fault` | typed `INTERNAL_ERROR` fault -> **502** |
| `unavailable` | backend crash (untyped fault) -> **502 LEGACY_UNAVAILABLE** |

## Test data (virtual schedule, computed from the clock)

The schedule (~80 flights over CDG / PEK / JFK / LHR / FRA / DXB / HND / SIN) is **not**
frozen at startup. Flights are stored as templates and materialised on every read:

* **Line flights** - departure = today's date + day offset + planned time of day, so dates
  roll forward automatically and the timetable never goes stale. Past slots of the current
  day show as `departed`, giving a natural mix of states at any hour.
* **Hot flights `HB900`-`HB907`** - always *now* + fixed minute offsets (29, 31, 45, 55, 90,
  150, 240, 360). The CHECKIN_CLOSED / boarding / check-in-open scenarios work no matter how
  long the app has been running.
* **Status derivation** - on every read: `cancelled` > departed > boarding (< 25 min) >
  check-in open (< 60 min) > scheduled.
* Exactly one line flight is cancelled (discoverable via `GET /api/v1/flights`).

Demo bookings are re-created on every restart:

| Ref | Passenger | Scenario |
|---|---|---|
| `GZT001` | John Doe | tomorrow-bound economy - happy-path check-in & bags |
| `QWX452` | Alice Martin | business, check-in open (~45 min) - overweight tests (limit 30 kg) |
| `LMN789` | Bob Chen | economy departing in ~29 min - triggers `CHECKIN_CLOSED` |
| `PRS205` | Carol Dupont | first class, already checked in - triggers `ALREADY_CHECKED_IN`, ready for bag drop |
| `TRV310` | David Wang | generic update/delete target |

Bookings live in memory only (max 50, oldest evicted); restarting resets them.

### Determinism contract

Everything except clock-derived dates/statuses is deterministic, so automated tests
can assert on exact values:

| Value | Guarantee |
|---|---|
| demo booking refs | fixed: `GZT001`, `QWX452`, `LMN789`, `PRS205`, `TRV310` |
| auto-generated booking refs | sequential per process: `T00001`, `T00002`, ... |
| flight numbers / routes / gates / aircraft | fixed by the schedule template (HB100..., HB900-HB907) |
| hot-flight offsets | fixed minutes from now: 29/31/45/55/90/150/240/360 |
| cancelled flight | exactly one line flight, always the same template slot |
| seat assignment | pure function of booking ref + cabin (`GZT001` -> always `31E`) |
| boarding sequences | live check-ins count from 201; seeded PRS205 keeps 101 |
| bag tags | sequential per process: `HB-00000001`, `HB-00000002`, ... |
| warning strings / fault codes | fixed formats documented above |

The only values that move are dates and derived statuses - by design.

## OpenAPI metadata for code generators

Both projects emit XML documentation files that Swashbuckle folds into the OpenAPI
document, so generated clients and Aethrix object trees carry descriptions and examples:

* every controller action documents summary, parameters (`<param>`) and per-status
  responses (`<response>` with the backend fault code),
* every DTO property carries `<summary>` plus an `<example>` value
  (e.g. `Flight.flightNumber -> "HB900"`, `Warning.code -> "CHECKED_BAGGAGE_OVERWEIGHT"`),
* enums describe when each state applies.

Inspect `/swagger/v1/swagger.json` to see `description` / `example` fields inline.

## Sample calls

```bash
# create a booking - flight is a structured designator {carrier enum, number}
curl -X POST http://localhost:5000/api/v1/bookings -H "Content-Type: application/json" \
  -d '{"flight":{"carrier":"hb","number":100},"cabinClass":"business","passenger":{"firstName":"Ada","lastName":"Lovelace","passport":"P1234567"}}'

# one-shot check-in with a polymorphic bag array (at most one of each type)
curl -X POST http://localhost:5000/api/v1/bookings/GZT001/check-in -H "Content-Type: application/json" \
  -d '{"bags":[{"type":"checked","weightKg":22,"color":"blue","lengthCm":70,"widthCm":45,"heightCm":28,"fragile":false},{"type":"carryOn","weightKg":7,"color":"grey","hasLaptop":true,"fitsUnderSeat":false}]}'

# single-bag drop after check-in (polymorphic single item, also enforces one-per-type)
curl -X POST http://localhost:5000/api/v1/bookings/GZT001/baggage -H "Content-Type: application/json" \
  -d '{"type":"checked","weightKg":31,"color":"black","lengthCm":75}'

# raw SOAP one-shot check-in with xsi:type polymorphic array
curl -X POST http://localhost:5000/soap/luggage -H "SOAPAction: \"http://hummingbird.airlines/luggage/CheckIn\"" \
  -H "Content-Type: text/xml; charset=utf-8" \
  -d '<?xml version="1.0"?><soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:lug="http://hummingbird.airlines/luggage"><soapenv:Body><lug:CheckIn><request xmlns:d4p1="http://schemas.datacontract.org/2004/07/Hummingbird.Airlines.Backend.Services" xmlns:b="http://schemas.datacontract.org/2004/07/Hummingbird.Airlines.Backend.Domain" xmlns:i="http://www.w3.org/2001/XMLSchema-instance"><d4p1:BookingRef>QWX452</d4p1:BookingRef><d4p1:Bags><b:Baggage i:type="b:CheckedBaggage"><b:WeightKg>22</b:WeightKg><b:LengthCm>70</b:LengthCm></b:Baggage><b:Baggage i:type="b:CarryOnBaggage"><b:WeightKg>7</b:WeightKg><b:HasLaptop>true</b:HasLaptop></b:Baggage></d4p1:Bags></request></lug:CheckIn></soapenv:Body></soapenv:Envelope>'
```

## License & legal notices

* **License** - released under the [MIT License](LICENSE).
  Copyright &copy; 2018&ndash;2026 Huaxing YUAN (first commit: 2018-06-11).
* **Trademark** - *Aethrix*&reg; and the Aethrix logo are registered trademarks of
  **Huaxing YUAN** (individual registration, European Patent Office).
* **Intended use** - this public test server exists solely for understanding, learning,
  practice and API testing (e.g. with Aethrix).
* **No abuse** - large-scale load generation, attack traffic, scraping at volume or any
  illegal use of the live endpoints is prohibited. Rate limiting is enforced per IP and
  the operator reserves the right to block offenders without notice.
* **No warranty** - the service is provided "as is" (see MIT license); data is in-memory
  and may be reset or become unavailable at any time.

## Disclaimer

Hummingbird Airlines is a fictional airline used for software testing demonstrations.

