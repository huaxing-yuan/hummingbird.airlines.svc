using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class LuggageRegistrationRequest
    {

        [DataMember(IsRequired = true)]
        public FlightInformation Flight { get; set; }

        [DataMember(IsRequired = true)]
        PassengerNameRecord Passenger { get; set; }

        [DataMember]
        Luggage[] Luggages { get; set; }
    }
}
