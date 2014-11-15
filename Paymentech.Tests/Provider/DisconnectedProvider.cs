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
    public class DisconnectedProvider:PaymentechGatewayProvider
    {
        public OrderInfo Order { get; set; }
        List<OrderItemInfo> _recurringItems;
        List<OrderItemInfo> _orderItems;
        public int SelectedProfileId=0;
        protected override int GetSelectedProfileId()
        {
            return SelectedProfileId;
        }
        public List<OrderItemInfo> OrderItems
        {
            get
            {
                return _orderItems ?? (_orderItems = new List<OrderItemInfo>());
            }
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
        protected override List<OrderItemInfo> GetNonRecurringOrderItems(OrderInfo orderInfo)
        {
            return OrderItems;
        }
        Random rand = new System.Random();
        protected override ProfileResponse CreateRecurringCustomerProfile(RecurringCustomerProfile recurringProfile, OrderItemInfo recurringOrderItem)
        {
            
            var response = new ProfileResponse();
            if (response.Success)
            {

               // recurringProfile.BillingAddressInfo = recurringProfile.BillingAddressInfo;
                //recurringProfile.CardInfo = recurringProfile.CardInfo;
                recurringProfile.MerchantId = "239880";
                //recurringProfile.CustomerRefNum = recurringProfile.CustomerRefNum;
                //recurringProfile.EmailAddress = recurringProfile.EmailAddress;
                recurringProfile.CustomerRefNum = rand.Next(11111111,99999999).ToString();
            }
            var profile = MapPaymentechProfileItem(recurringProfile, GetOrderInfo().OrderCustomerID, recurringOrderItem.OrderItemID);
            InsertPaymentechProfileItem(profile);
            return response;
        }

        protected override ProfileResponse CreateCustomerProfile(CustomerProfile profile)
        {
            var response = new ProfileResponse();
            if (response.Success)
            {

                //profile.BillingAddressInfo = recurringProfile.BillingAddressInfo;
                //profile.CardInfo = recurringProfile.CardInfo;
                profile.MerchantId = "239877";
                //profile.CustomerRefNum = recurringProfile.CustomerRefNum;
                //profile.EmailAddress = recurringProfile.EmailAddress;
                profile.CustomerRefNum = rand.Next(11111111, 99999999).ToString();
                
            }
            var pp = MapPaymentechProfileItem(profile, GetOrderInfo().OrderCustomerID);
            InsertPaymentechProfileItem(pp);
            response.CustomerRefNum = profile.CustomerRefNum;
            response.BillingAddressInfo = profile.BillingAddressInfo;
            response.CardInfo = profile.CardInfo;
            response.EmailAddress = profile.EmailAddress;
            response.MerchantId = profile.MerchantId;
            return response;
        }
        protected override PaymentechProfileItem MapPaymentechProfileItem(CustomerProfile profile, int customerID, int? recurringOrderItemID = null)
        {
            
            var result = new PaymentechProfileItem(); 
            result.CardBrand = profile.CardInfo.CardBrand;
            result.CardholderName = profile.CardInfo.CardholderName;
            result.CustomerRefNum = long.Parse(profile.CustomerRefNum);
            result.Expiration = profile.CardInfo.ExpirationDate;
            result.IsActive = true;
            result.MaskedAccountNumber = profile.CardInfo.CardNumber.Substring(profile.CardInfo.CardNumber.Length-4);
            result.MerchantID = long.Parse(profile.MerchantId);
            result.CustomerID = customerID;
            result.LastTransactionUtc = System.DateTime.UtcNow;
            if (recurringOrderItemID.HasValue)
                result.RecurringOrderItemID = recurringOrderItemID.Value;
            return result;
        }

        protected override OrderResponse CreateNewOrder(CustomerInfo customerInfo, OrderInfo orderInfo, PaymentechProfileItem paymentProfile)
        {
            var request = MapNewOrderRequest(paymentProfile, orderInfo);
            var result = new OrderResponse();
            result.TransactionType = GetShippingRequired(orderInfo) ? "A" : "AC";
            result.TransactionRefNum = Guid.NewGuid().ToString();
            result.AuthorizationCode = rand.Next(1111, 9999).ToString();
            result.CustomerRefNum = paymentProfile.CustomerRefNum.ToString();
            result.GatewayOrderId = Guid.NewGuid().ToString().Replace("-","").Substring(0, 22);
            result.HostResponseCode = "100";
            result.MerchantId = paymentProfile.MerchantID.ToString();
            result.PaymentStatus = result.TransactionType == "A" ? PaymentStatus.Authorized : PaymentStatus.AuthorizedForCapture;
            result.TransactionRequest = new System.Xml.Linq.XElement("request");
            result.TransactionResponse = new System.Xml.Linq.XElement("response");
            InsertPaymentechTransactionItem(customerInfo, orderInfo, result, paymentProfile);
            return result;
        }
    }
}
