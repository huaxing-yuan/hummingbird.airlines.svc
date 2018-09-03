using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class FlightInformation
    {
        [DataMember(IsRequired = true)]
        public string ID { get; set; }

        [DataMember(IsRequired = true)]
        public AirlineCodes AirlineCode { get; set; }

        [DataMember(IsRequired = true)]
        public string AirlineName { get; set; }

        [DataMember(IsRequired = true)]
        public uint FlightNumber { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime ExpectedDepartureTime { get; set; }

        [DataMember]
        public DateTime ExpectedArrivalTime { get; set; }

        [DataMember(IsRequired = true)]
        public AirportCodes DepartureAirport { get; set; }

        [DataMember(IsRequired = true)]
        public AirportCodes ArrivalAirport { get; set; }

        [DataMember(IsRequired = true)]
        public string DepartureTerminal { get; set; }

        [DataMember]
        public string ArrivalTerminal { get; set; }

        [DataMember]
        public string BoardingGate { get; set; }

        [DataMember]
        public string ArrivalGate { get; set; }

        [DataMember(IsRequired = true)]
        public Airliner Aircraft { get; set; }

        [DataMember]
        public string ExpectedRunway { get; set; }

        [DataMember(IsRequired = true)]
        public FlightStatus Status { get; set; }

    }
}
