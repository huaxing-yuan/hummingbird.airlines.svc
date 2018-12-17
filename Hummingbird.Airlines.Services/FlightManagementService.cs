using Hummingbird.Airlines.Services;
using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Hummingbird.Airlines.Services
{
    class FlightManagementService : IFlightManagement
    {
        public FlightInformation FlightInfo(FlightInfoRequest request)
        {
            return new FlightInformation()
            {
                Aircraft = new Airliner()
                {
                    DeliveryDate = DateTime.Now.AddYears(-10),
                    Manufacture = AircraftManufactures.BOEING,
                    PassengerCapacity = 368,
                    TailNumber = "B-2412",
                    Type = "777-300ER",
                },
                BoardingGate = "B-20",
                ArrivalGate = "89",
                ID = Guid.NewGuid().ToString(),
                DepartureAirport = request.Airport,
                AirlineCode = request.Airline,
                FlightNumber = request.FlightNumber,
                ExpectedArrivalTime = DateTime.Now.AddDays(1),
                ArrivalAirport = AirportCodes.CDG,
                Status = FlightStatus.ON_BOARDING,
                AirlineName = GetAirlineName(request.Airline),
            };
        }

        static string GetAirlineName(AirlineCodes code)
        {
            switch (code)
            {
                case AirlineCodes.AA:
                    return "American Airlines";
                case AirlineCodes.AF:
                    return "Air France";
                case AirlineCodes.BA:
                    return "British Airways";
                case AirlineCodes.CA:
                    return "Air China";
                case AirlineCodes.HA:
                    return "Hummingbird Airlines";
                case AirlineCodes.LH:
                    return "Lufthansa";
                case AirlineCodes.MH:
                    return "Malaysia Airlines";
                case AirlineCodes.NH:
                    return "All Nippon Airways";
                case AirlineCodes.SQ:
                    return "Singapore Airlines";
                case AirlineCodes.SU:
                    return "Aeroflot Russian Airlines";
                default:
                    return "Unknown";
            }
        }
    }
}
