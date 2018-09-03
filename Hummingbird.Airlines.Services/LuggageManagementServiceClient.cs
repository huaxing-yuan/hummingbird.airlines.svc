using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Hummingbird.Airlines.Services.BackendStructures;

namespace Hummingbird.Airlines.Services
{
    class LuggageManagementServiceClient : ClientBase<ILuggageManagement>, ILuggageManagement
    {
        public Reply Registration(LuggageRegistrationRequest request)
        {
            return base.Channel.Registration(request);
        }
    }
}
