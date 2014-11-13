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
        List<OrderItemInfo> _recurringItems;
        public List<OrderItemInfo> RecurringItems
        {
            get
            {
                return _recurringItems ?? (_recurringItems = new List<OrderItemInfo>());
            }
        }
        public CustomerProfile MapCustomerProfileAccessor(CustomerInfo customerInfo, OrderInfo orderInfo)
        {
            return this.MapCustomerProfile(customerInfo, orderInfo);
        }
        public RecurringCustomerProfile MapProfileRecurringInfoAccessor(CustomerInfo customerInfo, OrderInfo orderInfo,OrderItemInfo orderItemInfo)
        {
            return this.MapProfileRecurringInfo(customerInfo, orderInfo, orderItemInfo);
        }
        public ProfileResponse CreateCustomerProfileAccessor(CustomerProfile profile)
        {
            return this.CreateCustomerProfile(profile);
        }
        public List<PaymentechProfileItem> GetCustomerPaymentProfilesAccessor(CustomerInfo customerInfo)
        {
            return this.GetCustomerPaymentProfiles(customerInfo);
        }

        public ProfileResponse CreateRecurringCustomerProfileAccessor(RecurringCustomerProfile recurringProfile, OrderItemInfo recurringOrderItem)
        {
            return this.CreateRecurringCustomerProfile(recurringProfile,recurringOrderItem);
        }
        protected override OrderInfo GetOrderInfo()
        {
            return Order;
        }

        public OrderResponse CreateNewOrderAccessor(CustomerInfo customerInfo, OrderInfo orderInfo, PaymentechProfileItem paymentProfile)
        {
            return this.CreateNewOrder(customerInfo, orderInfo, paymentProfile);
        }

        public OrderResponse VoidOrderTransactionAccessor(CustomerInfo customerInfo, OrderInfo orderInfo)
        {
            return this.VoidOrderTransaction(customerInfo, orderInfo);
        }

        public OrderResponse RefundOrderTransactionAccessor(CustomerInfo customerInfo, OrderInfo orderInfo,double amount)
        {
            return this.RefundOrderTransaction(customerInfo, orderInfo,amount);
        }

        public ProfileResponse FetchCustomerProfileAccessor(PaymentechProfileItem paymentechProfile)
        {
            return this.FetchCustomerProfile(paymentechProfile);
        }
        protected override List<OrderItemInfo> GetRecurringOrderItems(OrderInfo orderInfo)
        {
            return RecurringItems;
        }
    }
}
