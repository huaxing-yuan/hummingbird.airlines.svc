using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class Airliner
    {
        [DataMember]
        public AircraftManufactures Manufacture { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string TailNumber { get; set; }

        [DataMember]
        public DateTime DeliveryDate { get; set; }

        [DataMember]
        public uint PassengerCapacity { get; set; }
    }
}
