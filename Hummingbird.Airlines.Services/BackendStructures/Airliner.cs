using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class Airliner
    {
        [DataMember(IsRequired = true)]
        public AircraftManufactures Manufacture { get; set; }

        [DataMember(IsRequired = true)]
        public string Type { get; set; }

        [DataMember(IsRequired = true)]
        public string TailNumber { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime DeliveryDate { get; set; }

        [DataMember(IsRequired = true)]
        public uint PassengerCapacity { get; set; }
    }
}
