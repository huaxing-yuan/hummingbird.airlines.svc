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
    public enum SeatClass
    {
        [DataMember]
        FIRST_CLASS,
        [DataMember]
        BUSINESS_CLASS,
        [DataMember]
        PREMIUM_ECONOMY,
        [DataMember]
        ECONOMY_CLASS
    }
}
