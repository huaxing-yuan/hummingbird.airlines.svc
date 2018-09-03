using Hummingbird.Airlines.Services.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.Services
{
    [ServiceContract(Name = "IFlightManagement", Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public interface IFlightManagement
    {
        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/BE/FlightInfo")]
        FlightInformation FlightInfo(FlightInfoRequest request);
    }
}
