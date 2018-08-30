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
        [EnumMember]
        FIRST_CLASS,
        [EnumMember]
        BUSINESS_CLASS,
        [EnumMember]
        PREMIUM_ECONOMY,
        [EnumMember]
        ECONOMY_CLASS
    }
}
