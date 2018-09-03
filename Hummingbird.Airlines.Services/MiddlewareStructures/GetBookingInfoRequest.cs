using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Hummingbird.Airlines.Services.MiddlewareStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/ME")]
    public class GetBookingInfoRequest
    {
        [DataMember(IsRequired = true)]
        public string ReservationId { get; set; }


        [DataMember(IsRequired = true)]
        public string PassportId { get; set; }
    }
}