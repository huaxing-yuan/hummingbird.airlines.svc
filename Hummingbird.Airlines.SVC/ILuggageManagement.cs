using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Airlines.SVC
{
    [ServiceContract(Name = "ILuggageManagement", Namespace = "http://www.hummingbird-alm.com/example/airlines/BE")]
    public interface ILuggageManagement
    {
    }
}
