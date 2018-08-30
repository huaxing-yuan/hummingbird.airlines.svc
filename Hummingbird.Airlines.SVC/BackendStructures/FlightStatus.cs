using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    /// <summary>
    /// IATA Airport Codes
    /// </summary>
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public enum FlightStatus
    {
        [DataMember]
        PLANNED,
        [DataMember]
        ON_BOARDING,
        [DataMember]
        GATE_CLOSED,
        [DataMember]
        IN_FLIGHT,
        [DataMember]
        LANDED_TAXIING,
        [DataMember]
        ARRIVED,
        [DataMember]
        DELAYED,
        [DataMember]
        CANCELLED
    }
}
