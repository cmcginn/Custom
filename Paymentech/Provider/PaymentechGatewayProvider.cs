using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.EcommerceProvider;
using Paymentech.Core;
using CMS.Ecommerce;
using CMS.Globalization;
using CMS.CustomTables.Types;
using System.Globalization;
using Paymentech.Data;

namespace Paymentech.Provider
{
    public class PaymentechGatewayProvider : CMSPaymentGatewayProvider, IPaymentechGateway
    {

        public override void ProcessPayment()
        {
            var orderInfo = GetOrderInfo();
            var customerInfo = CustomerInfoProvider.GetCustomerInfo(orderInfo.OrderCustomerID);
            var orderStatuses = OrderStatusInfoProvider.GetOrderStatuses();
            var orderStatus = orderStatuses.Single(x => x.StatusName == "PaymentFailed");
            ProcessRecurringItems(customerInfo, orderInfo);
            var completed = orderStatuses.Single(x => x.StatusName == "Completed");
            if (GetNonRecurringOrderItems(orderInfo).Any())
            {
                var standardOrder = ProcessNonRecurringItems(customerInfo, orderInfo);
                if (standardOrder.TransactionType == "A")
                    orderStatus = orderStatuses.Single(x => x.StatusName == "PendingCapture");
                else
                    orderStatus = completed;
            }
            else
            {
                orderStatus = completed;
            }

            orderInfo.OrderStatusID = orderStatus.StatusID;
            orderInfo.OrderIsPaid = orderStatus == completed;
        }

        #region IPaymentGateway Implementation
        public void CapturePayment(OrderInfo orderInfo)
        {
            var transactionProvider = new PaymentechTransactionProvider();
            var transactions = transactionProvider.GetOrderTransactionsForOrder(orderInfo);
            var authorized = transactions.OrderBy(x => x.ItemCreatedWhen).Last();
            if (authorized.TransactionType != "A")
                throw new PaymentechProviderException("Last transaction must be an authorization transaction in order to mark for capture");
            if (String.IsNullOrEmpty(authorized.TransactionReference))
                throw new PaymentechProviderException("Missing Transaction Reference");
            var customerInfo = CustomerInfoProvider.GetCustomerInfo(orderInfo.OrderCustomerID);
            var paymentProfileProvider = new PaymentechProfileProvider();
            var paymentProfile = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo).SingleOrDefault(x => x.ItemID == authorized.PaymentProfileID);
            if (paymentProfile == null)
                throw new PaymentechProviderException(String.Format("Could not retrieve profile {0}. Capture payment could not be completed", authorized.PaymentProfileID));
            var request = MapPriorOrderRequest(customerInfo, orderInfo, authorized, paymentProfile);
            //need to adjust total if order contains recurring products
            request.TransactionTotal = GetNonRecurringOrderTotal(orderInfo);
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var response = facade.CaptureAuthPayment(request);
            if (!response.Success)
                throw new PaymentechProviderException(response.ErrorMessage);
            response.GatewayOrderId = request.GatewayOrderId;
            //if we are here, everything is good
            orderInfo.OrderStatusID = OrderStatusInfoProvider.GetOrderStatuses().Single(x => x.StatusName == "PaymentReceived").StatusID;
            orderInfo.OrderIsPaid = true;
            InsertPaymentechTransactionItem(customerInfo, orderInfo, response, paymentProfile);
        }
        public void CapturePayment(int orderId)
        {
            var orderInfo = OrderInfoProvider.GetOrderInfo(orderId);
            CapturePayment(orderInfo);
        }
        public void RefundOrder(OrderInfo orderInfo, double? amount = null)
        {
            var transactionProvider = new PaymentechTransactionProvider();
            var transactions = transactionProvider.GetOrderTransactionsForOrder(orderInfo);
            var customerInfo = CustomerInfoProvider.GetCustomerInfo(orderInfo.OrderCustomerID);
            var lastTxn = transactions.OrderBy(x => x.ItemCreatedWhen).Last();
            if (lastTxn.TransactionType != "A" && lastTxn.TransactionType != "AC" && lastTxn.TransactionType != "CAPTURE")
                throw new PaymentechProviderException("Last transaction must be in Authorized, Authorized for Capture, or Capture status in order to issue a refund.");
            var paymentProfileProvider = new PaymentechProfileProvider();
            var paymentProfile = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo).SingleOrDefault(x => x.ItemID == lastTxn.PaymentProfileID);
            if (paymentProfile == null)
                throw new PaymentechProviderException(String.Format("Could not retrieve profile {0}. Capture payment could not be completed", lastTxn.PaymentProfileID));
            if (amount.HasValue && amount.Value > orderInfo.OrderTotalPrice)
                throw new PaymentechProviderException("Refund amount cannot exceed order total");
            var request = MapPriorOrderRequest(customerInfo, orderInfo, lastTxn, paymentProfile);
            var orderStatuses = OrderStatusInfoProvider.GetOrderStatuses();
            var finalOrderStatus = orderStatuses.Single(x => x.StatusName == "Refunded");
            if (amount.HasValue)
            {
                request.TransactionTotal = amount.Value;
                finalOrderStatus = orderStatuses.Single(x => x.StatusName == "PartiallyRefunded"); 
            }
            else
                request.TransactionTotal = orderInfo.OrderTotalPrice;
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var response = facade.Refund(request);
            if (!response.Success)
                throw new PaymentechProviderException(response.ErrorMessage);

            //if we are here, everything is good
            orderInfo.OrderStatusID = finalOrderStatus.StatusID;
            orderInfo.OrderIsPaid = false;
            InsertPaymentechTransactionItem(customerInfo, orderInfo, response, paymentProfile);
        }
        public void RefundOrder(int orderId, double? amount = null)
        {
            var orderInfo = OrderInfoProvider.GetOrderInfo(orderId);
            RefundOrder(orderInfo, amount);
        }
        public void ProcessScheduledRecurringPayment(OrderInfo orderInfo,int initialOrderId)
        {
            var paymentProfileProvider = new PaymentechProfileProvider();
            var initialOrder = OrderInfoProvider.GetOrderInfo(initialOrderId);
            var customerInfo = CustomerInfoProvider.GetCustomerInfo(orderInfo.OrderCustomerID);
            var orderItems = OrderItemInfoProvider.GetOrderItems(orderInfo.OrderID);
            if (orderItems.Count != 1)
                throw new PaymentechProviderException("Recurring scheduled order should contain a single order item.");
            var orderItem = orderItems.Single();
            var initialOrderItems = OrderItemInfoProvider.GetOrderItems(initialOrder.OrderID);
            var initialOrderItem = initialOrderItems.SingleOrDefault(x=>x.OrderItemSKUID == orderItem.OrderItemSKUID);
            if (initialOrderItem == null)
                throw new PaymentechProviderException("Initial order recurring item sku id does not match new order item sku id.");

            var profiles = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo);
            var profile = profiles.SingleOrDefault(x => x.RecurringOrderItemID == initialOrderItem.OrderItemID && x.IsActive);
            if (profile == null)
                throw new PaymentechProviderException("Could not find active recurring billing profile for initial recurring order item.");

            //if we are here, all is well
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var response = facade.FetchProfile(profile.CustomerRefNum.ToString(),true);
            if(response.Success &! String.IsNullOrEmpty(response.CustomerRefNum))
            {
                var paymentReceivedStatus = OrderStatusInfoProvider.GetOrderStatuses().Single(x=>x.StatusName == "PaymentReceived");
                orderInfo.OrderStatusID = paymentReceivedStatus.StatusID;
                orderInfo.OrderIsPaid = true;
            }
            else
            {
                var paymentReceivedStatus = OrderStatusInfoProvider.GetOrderStatuses().Single(x => x.StatusName == "PaymentFailed");
                orderInfo.OrderStatusID = paymentReceivedStatus.StatusID;
                orderInfo.OrderIsPaid = false;
            }
            


        }
        public void ProcessScheduledRecurringPayment(int orderId,int initialOrderId)
        {
            var orderInfo = OrderInfoProvider.GetOrderInfo(orderId);
            ProcessScheduledRecurringPayment(orderInfo, initialOrderId);
        }
        public void CancelRecurringProfile(OrderItemInfo initialOrderItem)
        {
            var orderInfo = OrderInfoProvider.GetOrderInfo(initialOrderItem.OrderItemOrderID);
            var customerInfo = CustomerInfoProvider.GetCustomerInfo(orderInfo.OrderCustomerID);
            var paymentProfileProvider = new PaymentechProfileProvider();
            var profile = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo).SingleOrDefault(x => x.RecurringOrderItemID == initialOrderItem.OrderItemID && x.IsActive);
            if (profile == null)
                throw new PaymentechProviderException("Could not find payment profile for specified recurring order item.");
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var response = facade.CancelProfile(profile.CustomerRefNum.ToString(), true);
            if (response.Success)
            {
                profile.IsActive = false;
                paymentProfileProvider.UpdatePaymentechProfileItem(profile);

            }
            else
                throw new PaymentechProviderException(response.ErrorMessage);
        }

        public void VoidOrder(OrderInfo orderInfo)
        {
            var transactionProvider = new PaymentechTransactionProvider();
            var transactions = transactionProvider.GetOrderTransactionsForOrder(orderInfo);
            var customerInfo = CustomerInfoProvider.GetCustomerInfo(orderInfo.OrderCustomerID);
            var lastTxn = transactions.OrderBy(x => x.ItemCreatedWhen).Last();
            if (lastTxn.TransactionType != "A" && lastTxn.TransactionType != "AC")
                throw new PaymentechProviderException("Last transaction must be in Authorized, Authorized for Capture, in order to void.");
            var paymentProfileProvider = new PaymentechProfileProvider();
            var paymentProfile = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo).SingleOrDefault(x => x.ItemID == lastTxn.PaymentProfileID);
            if (paymentProfile == null)
                throw new PaymentechProviderException(String.Format("Could not retrieve profile {0}. Void payment could not be completed", lastTxn.PaymentProfileID));
            var request = MapPriorOrderRequest(customerInfo, orderInfo, lastTxn, paymentProfile);
            var orderStatuses = OrderStatusInfoProvider.GetOrderStatuses();
            var finalOrderStatus = orderStatuses.Single(x => x.StatusName == "Voided");
            request.TransactionTotal = GetNonRecurringOrderTotal(orderInfo);
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var response = facade.Void(request, false);
            if (!response.Success)
                throw new PaymentechProviderException(response.ErrorMessage);

            //if we are here, everything is good
            orderInfo.OrderStatusID = finalOrderStatus.StatusID;
            orderInfo.OrderIsPaid = false;
            InsertPaymentechTransactionItem(customerInfo, orderInfo, response, paymentProfile);
        }
        public void VoidOrder(int orderId)
        {
            var orderInfo = OrderInfoProvider.GetOrderInfo(orderId);
            VoidOrder(orderInfo);
        }

        public void UpdateProfile(PaymentechProfileItem profile, IAddress billingAddress,string cardholderName, int expirationMonth, int expirationYear)
        {
           
            var customerProfile = new CustomerProfile();
            customerProfile.CustomerRefNum = profile.CustomerRefNum.ToString();
            MapCustomerBillingAddress(customerProfile, billingAddress);
            customerProfile.CardInfo = new CardInfo();
            var month = expirationMonth.ToString().PadLeft(2,'0');
            var year = expirationYear.ToString();
            if(year.Length != 4)
                year = "20" + year;
            customerProfile.CardInfo.ExpirationDate = String.Format("{0}{1}", year, month);
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var recurring = profile.MerchantID.ToString() == PaymentechGatewaySettings.RecurringMerchantId;
            var response = facade.UpdateProfile(customerProfile,recurring);
            if (!response.Success)
                throw new PaymentechProviderException(response.ErrorMessage);
            profile.CardholderName = cardholderName;
            profile.Expiration = customerProfile.CardInfo.ExpirationDate;
            var paymentProfileProvider = new PaymentechProfileProvider();
            paymentProfileProvider.UpdatePaymentechProfileItem(profile);
        }
        #endregion
        #region Class Methods

        protected virtual OrderResponse ProcessNonRecurringItems(CustomerInfo customerInfo, OrderInfo orderInfo)
        {
            //if customer profile selected 
            var profileId = GetSelectedProfileId();
            var paymentProfileProvider = new PaymentechProfileProvider();
            long customerRefNumber = 0;
            PaymentechProfileItem profileItem = null;
            if (profileId == 0)
            {
                //create new profile
                var newProfile = MapCustomerProfile(customerInfo, orderInfo);
                var profile = CreateCustomerProfile(newProfile);
                customerRefNumber = long.Parse(profile.CustomerRefNum);
                profileItem = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo).Single(x => x.CustomerRefNum == customerRefNumber);
                //TODO:Exception
            }
            else
            {
                profileItem = paymentProfileProvider.GetCustomerPaymentProfiles(customerInfo).Single(x => x.ItemID == profileId);
            }
            var orderResponse = CreateNewOrder(customerInfo, orderInfo, profileItem);
            return orderResponse;
        }
        protected void ProcessRecurringItems(CustomerInfo customerInfo, OrderInfo orderInfo)
        {
            var recurringItems = GetRecurringOrderItems(orderInfo);
            recurringItems.ForEach(item => ProcessRecurringItem(customerInfo, orderInfo, item));
        }
        protected void ProcessRecurringItem(CustomerInfo customerInfo, OrderInfo orderInfo, OrderItemInfo recurringItem)
        {
            var request = MapProfileRecurringInfo(customerInfo, orderInfo, recurringItem);
            var response = CreateRecurringCustomerProfile(request, recurringItem);

        }
        protected virtual OrderResponse CreateNewOrder(CustomerInfo customerInfo, OrderInfo orderInfo, PaymentechProfileItem paymentProfile)
        {
            var request = MapNewOrderRequest(paymentProfile, orderInfo);
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var result = facade.ProcessNewOrderPayment(request);
            InsertPaymentechTransactionItem(customerInfo, orderInfo, result, paymentProfile);
            return result;
        }
       
        protected virtual void InsertPaymentechProfileItem(PaymentechProfileItem paymentechProfile)
        {
            var profileProvider = new PaymentechProfileProvider();
            profileProvider.InsertPaymentechProfileItem(paymentechProfile);
        }
        protected virtual void InsertPaymentechTransactionItem(CustomerInfo customerInfo, OrderInfo orderInfo, OrderResponse orderResponse, PaymentechProfileItem profile)
        {

            var transaction = new PaymentechTransactionItem
            {
                AuthorizationCode = orderResponse.AuthorizationCode,
                CustomerID = customerInfo.CustomerID,
                HostResponseCode = orderResponse.HostResponseCode,
                IsSuccess = orderResponse.Success,
                MerchantID = long.Parse(orderResponse.MerchantId),
                OrderID = orderInfo.OrderID,
                ItemOrder = 0,
                PaymentProfileID = profile.ItemID,
                GatewayOrderID = orderResponse.GatewayOrderId,
                TransactionReference = orderResponse.TransactionRefNum,
                ProcStatusMessage = orderResponse.ProcStatusMessage,
                TransactionType = orderResponse.TransactionType,
                TransactionRequest = orderResponse.TransactionRequest.ToString(),
                TransactionResponse = orderResponse.TransactionResponse.ToString(),
                TransactionAmount = orderResponse.TransactionAmount
            };
            var provider = new PaymentechTransactionProvider();
            provider.InsertCustomerTransaction(transaction);

        }
        #endregion

        #region Gateway Access
        protected virtual List<PaymentechProfileItem> GetCustomerPaymentProfiles(CustomerInfo customerInfo)
        {
            var provider = new PaymentechProfileProvider();
            var result = provider.GetCustomerPaymentProfiles(customerInfo);
            return result;
        }
        protected virtual ProfileResponse CreateRecurringCustomerProfile(RecurringCustomerProfile recurringProfile, OrderItemInfo recurringOrderItem)
        {
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var response = facade.CreatePaymentechRecurringProfile(recurringProfile);
            if (response.Success)
            {

                recurringProfile.BillingAddressInfo = response.BillingAddressInfo;
                recurringProfile.CardInfo = response.CardInfo;
                recurringProfile.MerchantId = response.MerchantId;
                recurringProfile.CustomerRefNum = response.CustomerRefNum;
                recurringProfile.EmailAddress = response.EmailAddress;
            }
            var profile = MapPaymentechProfileItem(recurringProfile, GetOrderInfo().OrderCustomerID, recurringOrderItem.OrderItemID);
            InsertPaymentechProfileItem(profile);
            return response;
        }
        protected virtual ProfileResponse CreateCustomerProfile(CustomerProfile profile)
        {
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var result = facade.CreatePaymentechProfile(profile);
            if (result.Success)
            {

                profile.BillingAddressInfo = result.BillingAddressInfo;
                profile.CardInfo = result.CardInfo;
                profile.MerchantId = result.MerchantId;
                profile.CustomerRefNum = result.CustomerRefNum;
                profile.EmailAddress = result.EmailAddress;


                var pp = MapPaymentechProfileItem(profile, GetOrderInfo().OrderCustomerID);
                InsertPaymentechProfileItem(pp);
            }
            else
            {
                throw new PaymentechProviderException(result.ErrorMessage);
            }
            return result;
        }
        protected virtual ProfileResponse FetchCustomerProfile(PaymentechProfileItem paymentechProfile)
        {
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var recurring = paymentechProfile.MerchantID.ToString() == PaymentechGatewaySettings.RecurringMerchantId;
            var result = facade.FetchProfile(paymentechProfile.CustomerRefNum.ToString(), recurring);
            return result;
        }
        #endregion
        #region Mapping
        protected virtual CardInfo MapCustomerCardInfo()
        {
            var result = new CardInfo();
            result.CardBrand = "VISA";
            result.CardholderName = "Good RG Customer";
            result.CardNumber = "4112344112344113";
            result.CCV = "123";
            result.ExpirationDate = "022015";
            return result;
        }

        //protected virtual 
        protected virtual void MapCustomerBillingAddress(CustomerProfile customerProfile, IAddress billingAddress)
        {
            customerProfile.BillingAddressInfo = new BillingAddressInfo();
            customerProfile.BillingAddressInfo.Address1 = billingAddress.AddressLine1;
            customerProfile.BillingAddressInfo.Address2 = billingAddress.AddressLine2;
            customerProfile.BillingAddressInfo.City = billingAddress.AddressCity;
            var countryInfo = CountryInfoProvider.GetCountryInfo(billingAddress.AddressCountryID);
            if (countryInfo != null)
                customerProfile.BillingAddressInfo.Country = countryInfo.CountryTwoLetterCode;

            var stateProvince = StateInfoProvider.GetStateInfo(billingAddress.AddressStateID);
            if (stateProvince != null)
                customerProfile.BillingAddressInfo.StateProvince = stateProvince.StateCode;
            customerProfile.BillingAddressInfo.PhoneNumber = billingAddress.AddressPhone;
            customerProfile.BillingAddressInfo.PostalCode = billingAddress.AddressZip;
            customerProfile.BillingAddressInfo.BillingAddressName = billingAddress.AddressPersonalName;
        }
        protected virtual CustomerProfile MapCustomerProfile(CustomerInfo customerInfo, OrderInfo orderInfo)
        {

            var result = new CustomerProfile();

            result.CardInfo = MapCustomerCardInfo();
            result.EmailAddress = customerInfo.CustomerEmail;

            if (orderInfo.OrderBillingAddress != null)
            {
                MapCustomerBillingAddress(result, orderInfo.OrderBillingAddress);
            }
            return result;
        }
        protected virtual RecurringCustomerProfile MapProfileRecurringInfo(CustomerInfo customerInfo, OrderInfo orderInfo, OrderItemInfo recurringOrderItemInfo)
        {
            var custProfile = MapCustomerProfile(customerInfo, orderInfo);
            var result = new RecurringCustomerProfile();
            result.BillingAddressInfo = custProfile.BillingAddressInfo;
            result.CardInfo = custProfile.CardInfo;
            result.EmailAddress = custProfile.EmailAddress;
            result.MerchantId = PaymentechGatewaySettings.RecurringMerchantId;

            //TODO: Figure this out
            result.RecurringFrequency = RecurringFrequency.Monthly;
            result.RecurringAmount = recurringOrderItemInfo.OrderItemUnitPrice;
            result.StartDate = System.DateTime.UtcNow.AddDays(1);
            return result;
        }
        protected virtual PaymentechProfileItem MapPaymentechProfileItem(CustomerProfile profile, int customerID, int? recurringOrderItemID = null)
        {
            var result = new PaymentechProfileItem();
            result.CardBrand = profile.CardInfo.CardBrand;
            result.CardholderName = profile.CardInfo.CardholderName;
            result.CustomerRefNum = long.Parse(profile.CustomerRefNum);
            result.Expiration = profile.CardInfo.ExpirationDate;
            result.IsActive = true;
            result.MaskedAccountNumber = profile.CardInfo.MaskedCardNumber;
            result.MerchantID = long.Parse(profile.MerchantId);
            result.CustomerID = customerID;
            result.LastTransactionUtc = System.DateTime.UtcNow;
            if (recurringOrderItemID.HasValue)
                result.RecurringOrderItemID = recurringOrderItemID.Value;
            return result;
        }
        protected virtual PriorOrderRequest MapPriorOrderRequest(CustomerInfo customerInfo, OrderInfo orderInfo, PaymentechTransactionItem transaction, PaymentechProfileItem profile)
        {
            var result = new PriorOrderRequest();
            result.AuthorizationCode = transaction.AuthorizationCode;
            result.CustomerRefNum = profile.CustomerRefNum.ToString();
            result.GatewayOrderId = transaction.GatewayOrderID;
            result.TransactionRefNum = transaction.TransactionReference;

            return result;

        }
        protected virtual OrderRequest MapNewOrderRequest(PaymentechProfileItem paymentProfile, OrderInfo orderInfo)
        {
            var result = new OrderRequest();
            result.CustomerRefNum = paymentProfile.CustomerRefNum.ToString(CultureInfo.InvariantCulture);
            result.GatewayOrderId = CreateGatewayOrderId(orderInfo);
            result.OrderShipping = orderInfo.OrderTotalShipping;
            result.OrderTax = orderInfo.OrderTotalTax;
            result.TransactionTotal = GetNonRecurringOrderTotal(orderInfo);
            var orderItems = GetNonRecurringOrderItems(orderInfo);
            result.Comments = GetNewOrderComments(orderItems);
            result.ShippingRequired = GetShippingRequired(orderInfo);

            return result;
        }
        #endregion

        #region Class Properties
        PaymentechGatewaySettings _paymentechGatewaySettings;
        protected virtual PaymentechGatewaySettings PaymentechGatewaySettings
        {
            get
            {
                if (_paymentechGatewaySettings == null)
                {
                    _paymentechGatewaySettings = new PaymentechGatewaySettings
                    {
                        Bin = "000001",
                        GatewayFailoverUrl = "https://wsvar2.paymentech.net/PaymentechGateway",
                        GatewayUrl = "https://wsvar2.paymentech.net/PaymentechGateway",
                        MerchantId = "239877",
                        Password = "1X1T54HBRG3",
                        RecurringMerchantId = "239880",
                        SandboxGatewayFailoverUrl = "https://wsvar2.paymentech.net/PaymentechGateway",
                        SandboxGatewayUrl = "https://wsvar.paymentech.net/PaymentechGateway",
                        TerminalId = "001",
                        Username = "TMCGINN5353",
                        UseSandbox = true

                    };

                }
                return _paymentechGatewaySettings;
            }
        }

        #endregion
        #region Utility
        /// <summary>
        /// Included for testability
        /// </summary>
        /// <returns></returns>
        protected virtual OrderInfo GetOrderInfo()
        {
            return this.mOrder;
        }
        protected virtual string CreateGatewayOrderId(OrderInfo orderInfo)
        {
            return orderInfo.OrderGUID.ToString().Replace("-", "").Substring(0, 22);
        }
        public bool GetShippingRequired(OrderInfo orderInfo)
        {
            var result = false;
            var orderItemInfos = OrderItemInfoProvider.GetOrderItems(orderInfo.OrderID);
            result = orderItemInfos.Any(x => x.OrderItemSKU.SKUNeedsShipping);
            return result;
        }
        public string GetNewOrderComments(List<OrderItemInfo> orderItems)
        {
            var itemList = orderItems.Select(x => String.Format("{0} - {1}", x.OrderItemSKUID, x.OrderItemSKU)).ToList();
            var result = String.Join("|", itemList);
            return result;
        }
        protected virtual List<OrderItemInfo> GetNonRecurringOrderItems(OrderInfo orderInfo)
        {
            var nonRecurring = OrderItemInfoProvider.GetOrderItems(orderInfo.OrderID);
            var recurring = GetRecurringOrderItems(orderInfo);
            var result = nonRecurring.Except(recurring).ToList();
            return result;

        }
        protected virtual List<OrderItemInfo> GetRecurringOrderItems(OrderInfo orderInfo)
        {
            var recurring = OrderItemInfoProvider.GetOrderItems(orderInfo.OrderID);
            var nonRecurring = GetNonRecurringOrderItems(orderInfo);
            var result = recurring.Except(nonRecurring).ToList();
            return result;
        }
        protected double GetNonRecurringOrderTotal(OrderInfo orderInfo)
        {
            var recurringTotal = GetRecurringOrderItems(orderInfo).Sum(x => x.OrderItemUnitPrice * x.OrderItemUnitCount);
            return orderInfo.OrderTotalPrice - recurringTotal;
        }
        protected virtual int GetSelectedProfileId()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}
