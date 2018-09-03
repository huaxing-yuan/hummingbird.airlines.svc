using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services
{
    [ServiceContract(Name = "ILuggageManagement", Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public interface ILuggageManagement
    {
        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/BE/LuggageRegistration")]
        Reply Registration(LuggageRegistrationRequest request);
    }
}
