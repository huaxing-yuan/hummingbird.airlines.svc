using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Hummingbird.Airlines.ServiceHost.MiddlewareStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/api")]
    public enum Status
    {
        [EnumMember]
        OK,

        [EnumMember]
        Failure,

        [EnumMember]
        CriticalError


    }
}