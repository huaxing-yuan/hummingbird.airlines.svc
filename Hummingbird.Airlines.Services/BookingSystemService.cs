﻿using Hummingbird.Airlines.Services;
using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Hummingbird.Airlines.Services
{
    public class BookingSystemService : IBookingSystem
    {
        readonly BookingInformation bi = new BookingInformation()
        {
            AirlineName = "Air China",
            AirlineCode = AirlineCodes.CA,
            ArrivalAirport = AirportCodes.CDG,
            DepartureAirport = AirportCodes.PEK,
            BookingId = "9038723",
            ArrivalDateTime = DateTime.Now.AddDays(1),
            DepartureDateTime = DateTime.Now.AddHours(2),
            FlightNumber = 934,
            Passenger = new PassengerNameRecord()
            {
                FirstName = "John",
                LastName = "Doe",
                Passport = "A098234514",
                PNR_ID = Guid.NewGuid().ToString(),
            }
        };

        public BookingInformation GetBookingInfoById(string requestId)
        {
            bi.BookingId = requestId;
            return bi;
        }

        public BookingInformation GetBookingInfoByPassport(string passortId)
        {
            bi.Passenger.Passport = passortId;
            return bi;
        }
    }

}
