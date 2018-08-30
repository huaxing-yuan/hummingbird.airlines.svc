using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class BookingInformation
    {
        [DataMember]
        public string BookingId { get; set; }

        [DataMember]
        public AirlineCodes AirlineCode { get; set; }

        [DataMember]
        public string AirlineName { get; set; }

        [DataMember]
        public uint FlightNumber { get; set; }

        [DataMember]
        public DateTime DepartureDateTime { get; set; }

        [DataMember]
        public DateTime ArrivalDateTime { get; set; }

        [DataMember]
        public AirportCodes DepartureAirport { get; set; }

        [DataMember]
        public AirportCodes ArrivalAirport { get; set; }

        [DataMember]
        public PassengerNameRecord Passenger { get; set; }

        [DataMember]
        public SeatReservation Seat { get; set; }
    }
}
