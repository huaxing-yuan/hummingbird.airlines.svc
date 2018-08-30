using Hummingbird.Airlines.SVC;
using Hummingbird.Airlines.SVC.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Hummingbird.Airlines.ServiceHost
{
    class LuggageManagementService : ILuggageManagement
    {
        public Reply Registration(LuggageRegistrationRequest request)
        {
            return new Reply()
            {
                Success = true,
                ErrorCode = 0,
            };
        }
    }
}
