using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class PassengerNameRecord
    {
        [DataMember(IsRequired = true)]
        public string PNR_ID { get; set; }

        [DataMember(IsRequired = true)]
        public string Passport { get; set; }

        [DataMember(IsRequired = true)]
        public string FirstName { get; set; }

        [DataMember(IsRequired = true)]
        public string LastName { get; set; }
    }
}
