using Hummingbird.Airlines.Services;
using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Hummingbird.Airlines.Services
{
    public class LuggageManagementService : ILuggageManagement
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
