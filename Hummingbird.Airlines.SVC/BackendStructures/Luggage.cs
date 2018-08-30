using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class Luggage
    {
        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public LuggageTypes Type { get; set; }

        [DataMember]
        public string OwnerID { get; set; }

        [DataMember]
        public float Weight { get; set; }

        [DataMember]
        public LuggageSizes Size { get; set; }


    }
}
