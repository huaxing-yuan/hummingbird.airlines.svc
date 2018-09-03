using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public enum LuggageSizes
    {
        [EnumMember]
        XL,

        [EnumMember]
        L,

        [EnumMember]
        M,

        [EnumMember]
        S,

        [EnumMember]
        XS,

    }
}
