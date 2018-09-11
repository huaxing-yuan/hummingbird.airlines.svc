using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    [KnownType(typeof(CarryOnLuggage))]

    public abstract class AbstractLuggage
    {
        [DataMember(IsRequired = true)]
        public string ID { get; set; }

        [DataMember(IsRequired = true)]
        public string OwnerID { get; set; }

        [DataMember(IsRequired = true)]
        public float Weight { get; set; }

        [DataMember(IsRequired = true)]
        public LuggageSizes Size { get; set; }


    }
}
