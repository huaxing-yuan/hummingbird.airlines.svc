using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class FlightInformation
    {
        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public AirlineCodes AirlineCode { get; set; }

        [DataMember]
        public string AirlineName { get; set; }

        [DataMember]
        public uint FlightNumber { get; set; }

        [DataMember]
        public DateTime ExpectedDepartureTime { get; set; }

        [DataMember]
        public DateTime ExpectedArrivalTime { get; set; }

        [DataMember]
        public AirportCodes DepartureAirport { get; set; }

        [DataMember]
        public AirportCodes ArrivalAirport { get; set; }

        [DataMember]
        public string DepartureTerminal { get; set; }

        [DataMember]
        public string ArrivalTerminal { get; set; }

        [DataMember]
        public string BoardingGate { get; set; }

        [DataMember]
        public string ArrivalGate { get; set; }

        [DataMember]
        public Airliner Aircraft { get; set; }

        [DataMember]
        public string ExpectedRunway { get; set; }

        [DataMember]
        public FlightStatus Status { get; set; }

    }
}
