using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Hummingbird.Airlines.Services.MiddlewareStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/ME")]
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