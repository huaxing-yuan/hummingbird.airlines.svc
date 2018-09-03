using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    /// <summary>
    /// IATA Airport Codes
    /// </summary>
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public enum FlightStatus
    {
        [EnumMember]
        PLANNED,
        [EnumMember]
        ON_BOARDING,
        [EnumMember]
        GATE_CLOSED,
        [EnumMember]
        IN_FLIGHT,
        [EnumMember]
        LANDED_TAXIING,
        [EnumMember]
        ARRIVED,
        [EnumMember]
        DELAYED,
        [EnumMember]
        CANCELLED
    }
}
