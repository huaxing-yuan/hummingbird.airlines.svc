using Hummingbird.Airlines.SVC.BackendStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC
{
    [ServiceContract(Name = "IFlightManagement", Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public interface IFlightManagement
    {
        [OperationContract(Action = "http://www.hummingbird-alm.com/example/airlines/BE/FlightInfo")]
        FlightInformation FlightInfo(AirlineCodes airlineCode, AirportCodes airportCode, string flightNumber);
    }
}
