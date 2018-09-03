using Hummingbird.Airlines.Services.MiddlewareStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services
{
    [ServiceContract(Name = "IEaiIntegrationServices", Namespace = "http://www.hummingbird-alm.com/example/airlines/ME")]
    public interface IEaiServices
    {
        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/ME/GetbookingInformation")]
        [FaultContract(typeof(EaiFault))]
        GetBookingInfoResponse GetBookingInformation(GetBookingInfoRequest request);

        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/ME/Registration")]
        [FaultContract(typeof(EaiFault))]
        void Registration(RegistrationRequest request);

        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/ME/PrintBoardingPass")]
        [FaultContract(typeof(EaiFault))]
        [FaultContract(typeof(PrinterFault))]
        void PrintBoardingPass(PrintBoardingPassRequest request);
    }
}
