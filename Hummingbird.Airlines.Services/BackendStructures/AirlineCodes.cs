using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    /// <summary>
    ///  Airline codes used in this example
    /// </summary>
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public enum AirlineCodes
    {
        /// <summary>
        /// Hummingbird Airlines
        /// </summary>
        [EnumMember]
        HA,
        /// <summary>
        /// American Airlines
        /// </summary>
        [EnumMember]
        AA,
        /// <summary>
        /// Air France
        /// </summary>
        [EnumMember]
        AF,
        /// <summary>
        /// British Airways
        /// </summary>
        [EnumMember]
        BA,
        /// <summary>
        /// Air China
        /// </summary>
        [EnumMember]
        CA,
        /// <summary>
        /// Lufthansa
        /// </summary>
        [EnumMember]
        LH,
        /// <summary>
        /// Malaysia Airlines
        /// </summary>
        [EnumMember]
        MH,
        /// <summary>
        /// Aeroflot Russian Airlines
        /// </summary>
        [EnumMember]
        SU,
        /// <summary>
        /// All Nippon Airways
        /// </summary>
        [EnumMember]
        NH,
        /// <summary>
        /// Singapore Airlines
        /// </summary>
        [EnumMember]
        SQ
    }
}
