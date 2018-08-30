using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC.BackendStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public class SeatReservation
    {
        [DataMember]
        public SeatClass Class { get; set; }

        [DataMember]
        public string SeatNumber { get; set; }

    }
}
