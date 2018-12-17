using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Hummingbird.Airlines.Services.MiddlewareStructures;

namespace Hummingbird.Airlines.Services
{
    public class EAIServices : IEaiServices
    {
        static EaiFault fault = new EaiFault()
        {
            Details = "This example service is not yet implemented",
            ErrorCode = -1,
        };
        public GetBookingInfoResponse GetBookingInformation(GetBookingInfoRequest request)
        {
            throw new FaultException<EaiFault>(fault);
        }

        public void PrintBoardingPass(PrintBoardingPassRequest request)
        {
            throw new FaultException<EaiFault>(fault);
        }

        public void Registration(RegistrationRequest request)
        {
            throw new FaultException<EaiFault>(fault);
        }
    }
}
