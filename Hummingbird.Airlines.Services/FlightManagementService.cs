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
                AirlineName = request.Airline.ToString(),
            };
        }
    }
}
