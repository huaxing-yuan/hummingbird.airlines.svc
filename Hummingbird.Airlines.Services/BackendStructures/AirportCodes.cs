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
    public enum AirportCodes
    {
        [EnumMember]
        CDG,
        [EnumMember]
        NRT,
        [EnumMember]
        PEK,
        [EnumMember]
        JFK,
        [EnumMember]
        JKT,
        [EnumMember]
        MOW,
        [EnumMember]
        BKK,
        [EnumMember]
        ATL,
        [EnumMember]
        DXB,
        [EnumMember]
        HKG,
        [EnumMember]
        LHR


    }
}
