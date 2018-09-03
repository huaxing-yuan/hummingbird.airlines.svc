using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services
{
    [ServiceContract(Name = "IBookingSystemContract", Namespace= "http://www.hummingbird-alm.com/example/airlines/BE")]
    public interface IBookingSystem
    {
        [OperationContract(Action= "http://www.hummingbird-alm.com/example/airlines/BE/GetBookingInfoById")]
        BookingInformation GetBookingInfoById(string requestId);

        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/BE/GetBookingInfoByPassport")]
        BookingInformation GetBookingInfoByPassport(string passortId);
    }
}
