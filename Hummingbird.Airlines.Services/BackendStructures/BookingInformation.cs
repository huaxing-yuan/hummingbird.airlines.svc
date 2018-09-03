using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class BookingInformation
    {
        [DataMember(IsRequired = true)]
        public string BookingId { get; set; }

        [DataMember(IsRequired = true)]
        public AirlineCodes AirlineCode { get; set; }

        [DataMember(IsRequired = true)]
        public string AirlineName { get; set; }

        [DataMember(IsRequired = true)]
        public uint FlightNumber { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime DepartureDateTime { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime ArrivalDateTime { get; set; }

        [DataMember(IsRequired = true)]
        public AirportCodes DepartureAirport { get; set; }

        [DataMember(IsRequired = true)]
        public AirportCodes ArrivalAirport { get; set; }

        [DataMember(IsRequired = true)]
        public PassengerNameRecord Passenger { get; set; }

        [DataMember]
        public SeatReservation Seat { get; set; }
    }
}
