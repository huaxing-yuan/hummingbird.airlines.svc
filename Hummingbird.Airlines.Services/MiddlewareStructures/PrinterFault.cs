using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services.MiddlewareStructures
{
    [DataContract(Namespace = "http://www.hummingbird-alm.com/example/airlines/ME")]
    public class PrinterFault
    {
        [DataMember(IsRequired =true)]
        public int PrinterReturnCode { get; set; }

        [DataMember(IsRequired = true)]
        public string OriginalCode { get; set; }

        [DataMember(IsRequired = true)]
        public string OriginalMessage { get; set; }

    }
}
