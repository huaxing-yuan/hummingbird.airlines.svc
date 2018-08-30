using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public enum AircraftManufactures
    {
        [DataMember]
        AIRBUS,
        [DataMember]
        BOEING,
        [DataMember]
        UAC,
        [DataMember]
        ANTONOV,
        [DataMember]
        BOMBARDIER,
        [DataMember]
        EMBRAER,
        [DataMember]
        COMAC,
        [DataMember]
        ATR
    }
}
