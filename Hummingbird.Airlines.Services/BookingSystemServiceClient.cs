using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Hummingbird.Airlines.Services.BackendStructures;

namespace Hummingbird.Airlines.Services
{
    internal class BookingSystemServiceClient : ClientBase<IBookingSystem>, IBookingSystem
    {
        public BookingInformation GetBookingInfoById(string requestId)
        {
            return base.Channel.GetBookingInfoById(requestId);
        }

        public BookingInformation GetBookingInfoByPassport(string passortId)
        {
            return base.Channel.GetBookingInfoByPassport(passortId);
        }
    }
}
