﻿using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.MiddlewareStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/ME")]
    public class PrintBoardingPassRequest
    {
        [DataMember(IsRequired = true)]
        FlightInformation FlightInfo { get; set; }

        [DataMember(IsRequired = true)]
        SeatReservation Seat { get; set; }

        [DataMember(IsRequired = true)]
        PassengerNameRecord Passenger { get; set; }
    }
}
