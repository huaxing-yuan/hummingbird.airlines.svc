using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Hummingbird.Airlines.ServiceHost.MiddlewareStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/api")]
    public class ApiFault
    {

        [DataMember(IsRequired =true)]
        public int ErrorCode { get; set; }

        [DataMember(IsRequired = true)]
        public string Details { get; set; }

    }
}