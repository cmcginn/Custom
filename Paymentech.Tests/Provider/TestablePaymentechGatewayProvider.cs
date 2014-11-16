using CMS.CustomTables.Types;
using CMS.Ecommerce;
using Paymentech.Provider;
using Paymentech.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Tests.Provider
{
    public class TestablePaymentechGatewayProvider : PaymentechGatewayProvider, IPaymentechGateway
    {
        public OrderInfo Order { get; set; }
        List<OrderItemInfo> _recurringItems;
        List<OrderItemInfo> _orderItems;
        public List<OrderItemInfo> OrderItems
        {
            get
            {
                return _orderItems ?? (_orderItems = new List<OrderItemInfo>());
            }
        }
        public int SelectedProfileId = 0;
        protected override int GetSelectedProfileId()
        {
            return SelectedProfileId;
        }
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





        public ProfileResponse FetchCustomerProfileAccessor(PaymentechProfileItem paymentechProfile)
        {
            return this.FetchCustomerProfile(paymentechProfile);
        }
        protected override List<OrderItemInfo> GetRecurringOrderItems(OrderInfo orderInfo)
        {
            return RecurringItems;
        }
        protected override List<OrderItemInfo> GetNonRecurringOrderItems(OrderInfo orderInfo)
        {
            return OrderItems;
        }
    }
}
