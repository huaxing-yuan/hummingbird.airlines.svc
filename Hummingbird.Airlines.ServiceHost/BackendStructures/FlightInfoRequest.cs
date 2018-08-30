using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class FlightInfoRequest
    {
        [DataMember(IsRequired = true)]
        public AirlineCodes Airline { get; set; }

        [DataMember(IsRequired = true)]
        public AirportCodes Airport { get; set; }
            
        [DataMember(IsRequired = true)]
        public uint FlightNumber { get; set; }
    }
}
