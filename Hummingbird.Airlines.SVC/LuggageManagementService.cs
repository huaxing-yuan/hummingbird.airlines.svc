using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hummingbird.Airlines.SVC.BackendStructures;

namespace Hummingbird.Airlines.SVC
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
