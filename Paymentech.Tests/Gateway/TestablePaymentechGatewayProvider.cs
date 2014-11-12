using CMS.CustomTables.Types;
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
        public RecurringCustomerProfile MapProfileRecurringInfoAccessor(CustomerInfo customerInfo, OrderInfo orderInfo,ShoppingCartItemInfo recurringShoppingCartItemInfo)
        {
            return this.MapProfileRecurringInfo(customerInfo, orderInfo, recurringShoppingCartItemInfo);
        }
        public ProfileResponse CreateCustomerProfileAccessor(CustomerProfile profile)
        {
            return this.CreateCustomerProfile(profile);
        }
        public List<PaymentechProfileItem> GetCustomerPaymentProfilesAccessor(CustomerInfo customerInfo)
        {
            return this.GetCustomerPaymentProfiles(customerInfo);
        }

        public ProfileResponse CreateRecurringCustomerProfileAccessor(RecurringCustomerProfile recurringProfile)
        {
            return this.CreateRecurringCustomerProfile(recurringProfile);
        }
        protected override OrderInfo GetOrderInfo()
        {
            return Order;
        }

        public OrderResponse CreateNewOrderAccessor(CustomerInfo customerInfo, OrderInfo orderInfo, PaymentechProfileItem paymentProfile)
        {
            return this.CreateNewOrder(customerInfo, orderInfo, paymentProfile);
        }
       
    }
}
