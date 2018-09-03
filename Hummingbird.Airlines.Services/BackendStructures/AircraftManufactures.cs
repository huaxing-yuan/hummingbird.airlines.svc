using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public enum AircraftManufactures
    {
        [EnumMember]
        AIRBUS,
        [EnumMember]
        BOEING,
        [EnumMember]
        UAC,
        [EnumMember]
        ANTONOV,
        [EnumMember]
        BOMBARDIER,
        [EnumMember]
        EMBRAER,
        [EnumMember]
        COMAC,
        [EnumMember]
        ATR
    }
}
