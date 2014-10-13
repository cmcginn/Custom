using CMS.Ecommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Tests.Mocks
{
    public class PaymentechSettingsMock
    {
        public OrderInfo OrderInfo { get; set; }

        internal int GetCustomerId()
        {
            return 71;
        }
    }
}
