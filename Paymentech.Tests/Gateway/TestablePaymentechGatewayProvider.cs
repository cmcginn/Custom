using CMS.Ecommerce;
using PaymentechGateway.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Tests.Gateway
{
    public class TestablePaymentechGatewayProvider:PaymentechGatewayProvider
    {
        public OrderInfo Order { get; set; }
        public CustomerProfile MapCustomerProfileAccessor(CustomerInfo customerInfo, OrderInfo orderInfo)
        {
            return this.MapCustomerProfile(customerInfo, orderInfo);
        }
        public ProfileResponse CreateCustomerProfileAccessor(CustomerProfile profile)
        {
            return this.CreateCustomerProfile(profile);
        }

        protected override OrderInfo GetOrderInfo()
        {
            return Order;
        }
       
    }
}
