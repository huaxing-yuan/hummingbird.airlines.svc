using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Hummingbird.Airlines.Services.BackendStructures;

namespace Hummingbird.Airlines.Services
{
    internal class FlightManagementServiceClient : ClientBase<IFlightManagement>, IFlightManagement
    {
        public FlightInformation FlightInfo(FlightInfoRequest request)
        {
            return base.Channel.FlightInfo(request);
        }
    }
}
