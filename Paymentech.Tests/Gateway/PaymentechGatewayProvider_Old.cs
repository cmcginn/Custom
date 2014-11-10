//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using CMS.CustomTables.Types;
//using CMS.Ecommerce;
//using CMS.EcommerceProvider;
//using Paymentech.Tests.Mocks;
//using Paymentech.Tests.PaymentechGateway;
//using System.ServiceModel;
//using CMS.Globalization;
//using CMS.DataEngine;


//namespace Paymentech.Tests.Gateway
//{
//    public class PaymentechGatewayProvider : CMSPaymentGatewayProvider
//    {
//        public const string BIN = "000001";
//        public const string TERMINAL_ID = "001";
//        public const string VERSION = "2.8";

//        #region Testablity helpers

//        public OrderInfo TestOrderInfo
//        {
//            get { return this.mOrder; }
//            set { this.mOrder = value; }
//        }
//        #endregion

//        public override void ProcessPayment()
//        {
//            var recurringProducts = GetRecurringProducts();

//        }

//        public void ProcessNewRecurringProfileResponse(ProfileResponseElement response)
//        {

//        }
//        public void ProcessNewOrderResponse(NewOrderResponseElement newOrderResponse)
//        {
//            var orderStatues = OrderStatusInfoProvider.GetOrderStatuses();

//            //if its not zero, it failed
//            if (newOrderResponse.procStatus == "0")
//            {

//                //authorized and captured, nothing left to do
//                if (newOrderResponse.transType == "AC")
//                    mOrder.OrderStatusID = orderStatues.Single(x => x.StatusName == "Completed").StatusID;
//                //authorization only, need to capture this at a later time
//                else if (newOrderResponse.transType == "A")
//                {
//                    //TODO: add pending capture status
//                    mOrder.OrderStatusID = orderStatues.Single(x => x.StatusName == "PendingCapture").StatusID;
//                    //need to save capture info
//                }
//            }
//            SaveNewTransaction(newOrderResponse);
//        }
//        public void ProcessPaymentForOrder()
//        {
//            var profiles = GetCustomerPaymentProfiles();
//            PaymentechProfileItem profile = null;
//            //customer has selected payment profile
//            if(GetPaymentProfileID().HasValue)
//            {
//                profile = profiles.Single(x => x.ItemID == GetPaymentProfileID().Value);
//            }else
//            {
//                //no existing profile selected, customer entered details, check if we have a profile for customer, to avoid duplication
//                profile = GetCustomerPaymentProfile(GetCardNumber(), GetCardExpiration());
//            }
//            //TODO:Update card num possibly same card number different expiration
//            //we have a new profile
//            if(profile==null)
//            {
//                var response = CreateProfile();
//                SaveNewProfile(response);
//                profiles = GetCustomerPaymentProfiles();
//                profile = profiles.Single();
//            }

//            var p = MapNewOrderRequestElement(profile.CustomerRefNum);
//            p.orbitalConnectionPassword = GetClientPassword();
//            p.orbitalConnectionUsername = GetClientUsername();

//            var c = GetClient();
//            var newOrderResponse = c.NewOrder(p);
            
//            ProcessNewOrderResponse(newOrderResponse);
            
//        }
//        public void ProcessPaymentForRecurringCart()
//        {
//            //always a new profile
//            //get cart item 
//            var item = GetRecurringProducts().First();
//            var profile = CreateRecurringProfile(item);
//            SaveNewProfile(profile,true);
    
            
//        }
       
//        #region Utilities

//        public string GetPaymentechDateString(DateTime source)
//        {
//            var result = source.ToString("MMddyyyy");
//            return result;
//        }
//        public void GetSku()
//        {
//            // mOrder.Order
//            //SKUInfoProvider.Ge
//            // mOrder.
//           //var x = ShoppingCartInfo.
            
//        }
//        #region Recurring Product Helpers

//        public double GetRecurringAmount(ShoppingCartItemInfo shoppingCartItemInfo)
//        {
//            return 10.00d;
//        }
//        public DateTime GetRecurringBillingEndDate(ShoppingCartItemInfo shoppingCartItemInfo)
//        {
//            //TODO:Handle start date
//            return System.DateTime.UtcNow.AddYears(99);
//        }
//        public DateTime GetRecurringBillingStartDate(ShoppingCartItemInfo shoppingCartItemInfo)
//        {
//            //TODO:Handle start date
//            return System.DateTime.UtcNow.AddDays(1);
//        }
//        public string GetRecurringSchedule(ShoppingCartItemInfo shoppingCartItemInfo)
//        {
//            return String.Format("{0} 1-12/{1} ?", (int)GetRecurringBillingStartDate(shoppingCartItemInfo).Day, 1);
//        }
//        public List<ShoppingCartItemInfo> GetRecurringProducts()
//        {
//            //TODO:Figure out what is recurring
//            //return new List<ShoppingCartItemInfo> { new ShoppingCartItemInfo() };
//            //return ShoppingCartItemInfoProvider.GetShoppingCartItems()
//            return ShoppingCartInfo.CartItems;
//        }
//        #endregion
       
//        private ShoppingCartInfo _shoppingCartInfo;
//        public ShoppingCartInfo ShoppingCartInfo { get
//        {
//            return _shoppingCartInfo ??
//                   (_shoppingCartInfo = ShoppingCartInfoProvider.GetShoppingCartInfoFromOrder(mOrder.OrderID));
//        } }
//        public List<PaymentechProfileItem> GetCustomerPaymentProfiles()
//        {
//            var result = PaymentechProfileProvider.GetCustomerPaymentProfiles(CustomerInfo);
//            return result;
//        }
//        public PaymentechProfileItem GetCustomerPaymentProfile(string accountNumber, string expiration)
//        {           
//            PaymentechProfileItem result = null;
//            if(!String.IsNullOrEmpty(accountNumber))
//            {
//                accountNumber = accountNumber.Substring(accountNumber.Length,4);
//                var profiles = GetCustomerPaymentProfiles();
//                result = profiles.Where(x => x.MaskedAccountNumber == accountNumber && x.Expiration == expiration).OrderByDescending(x => x.LastTransactionUtc).FirstOrDefault();
//            }
//            return result;
//        }
//        PaymentechProfileProvider _paymentechProfileProvider;
//        PaymentechProfileProvider PaymentechProfileProvider
//        {
//            get { return _paymentechProfileProvider ?? (_paymentechProfileProvider = new PaymentechProfileProvider()); }
//        }

//        private PaymentechTransactionProvider _paymentechTransactionProvider;

//        public PaymentechTransactionProvider PaymentechTransactionProvider
//        {
//            get
//            {
//                return _paymentechTransactionProvider ??
//                       (_paymentechTransactionProvider = new PaymentechTransactionProvider());
//            }
//        }

        
//        CustomerInfo _customerInfo;
//        CustomerInfo CustomerInfo
//        {
//            get { return _customerInfo ?? (_customerInfo = CustomerInfoProvider.GetCustomerInfo(mOrder.OrderCustomerID)); }
//        }
//        public string GetRecurringMid()
//        {
//            return "239880";
//        }
//        public string GetStandardMid()
//        {
//            return "239877";
//        }
//        public string GetClientUsername()
//        {
//            return "TMCGINN5353";
//        }


//        public string GetClientPassword()
//        {
//            return "1X1T54HBRG3";
//        }

//        public string GetPrimaryServiceUrl()
//        {
//            return "https://wsvar.paymentech.net/PaymentechGateway";
//        }
//        public string GetSecondaryServiceUrl()
//        {
//            return "https://wsvar2.paymentech.net/PaymentechGateway";
//        }
//        public int? GetPaymentProfileID()
//        {
//            return 8;
//        }
//        public string GetCardNumber()
//        {
//            return "4112344112344113";
//        }
//        public string GetCardBrand()
//        {
//            return "visa";
//        }
//        /// <summary>
//        /// Paymentech amount formats are strings with no decimal (i.e $10.00 = 1000)
//        /// </summary>
//        /// <param name="amount"></param>
//        /// <returns></returns>
//        public string GetAmount(double amount)
//        {
//            return amount.ToString(CultureInfo.InvariantCulture).Replace(".", "");
//        }
//        public string GetCardExpiration()
//        {
//            return "201807";
//        }
//        public string GetCardCcv()
//        {
//            return "411";
//        }
//        public PaymentechGatewayPortTypeClient GetClient()
//        {
//            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
//            EndpointAddress endpoint = new EndpointAddress(GetPrimaryServiceUrl());
//            var result = new PaymentechGatewayPortTypeClient(binding, endpoint);
//            return result;
//        }
//        public string GetNewCustomerRefNum()
//        {
//            var rand = new Random();
//            return rand.Next(1000000, 9999999).ToString();
//        }

//        ObjectQuery<OrderItemInfo> _orderItemInfos;
//        public ObjectQuery<OrderItemInfo> OrderItemInfos
//        {
//            get
//            {
//                return _orderItemInfos ?? (_orderItemInfos = OrderItemInfoProvider.GetOrderItems(mOrderId));
//            }
//        }


//        public bool GetShippingRequired()
//        {
//            var result = false;
//            result = OrderItemInfos.Any(x => x.OrderItemSKU.SKUNeedsShipping);
//            return result;
//        }
//        public string GetStateCode(int stateId)
//        {
//            string result = null;
//            var stateInfo = StateInfoProvider.GetStateInfo(stateId);
//            if (stateInfo != null)
//                result = stateInfo.StateCode;
//            return result;
//        }
//        public string GetCountryCode(int countryId)
//        {
//            string result = null;
//            var countryInfo = CountryInfoProvider.GetCountryInfo(countryId); 
//            if (countryInfo != null)
//                result = countryInfo.CountryTwoLetterCode;
//            return result;
//        }
//        public ProfileAddElement MapProfileAddElement()
//        {
            
//            var result = new ProfileAddElement();
//            result.version = "2.8";
//            result.orbitalConnectionPassword = GetClientPassword();
//            result.orbitalConnectionUsername = GetClientUsername();
//            result.bin = BIN;            
//            result.ccAccountNum = GetCardNumber();
//            result.ccExp = GetCardExpiration();
//            result.customerEmail = CustomerInfo.CustomerEmail;
//            result.customerAddress1 = mOrder.OrderBillingAddress.AddressLine1;
//            result.customerAddress2 = mOrder.OrderBillingAddress.AddressLine2;
//            result.customerCity = mOrder.OrderBillingAddress.AddressCity;
//            result.customerCountryCode = GetCountryCode(mOrder.OrderBillingAddress.AddressCountryID);
//            result.customerState = GetStateCode(mOrder.OrderBillingAddress.AddressStateID);
//            result.customerPhone = mOrder.OrderBillingAddress.AddressPhone;
//            result.customerZIP = mOrder.OrderBillingAddress.AddressZip;
//            result.customerProfileFromOrderInd = "NO";
//            result.customerRefNum = GetNewCustomerRefNum();
//            var eligibleAccountUpdaterBrands = new List<string> {"visa", "mastercard"};
//            var brand = GetCardBrand();
//            result.accountUpdaterEligibility = eligibleAccountUpdaterBrands.Contains(brand) ? "Y" : "N";
//            result.customerProfileOrderOverideInd = "OA";
//            result.customerProfileFromOrderInd = "A";
//            result.customerAccountType="CC";
//            return result;
            
//        }
//        public ProfileResponseElement CreateProfile()
//        {
//            var client = GetClient();
//            var request = MapProfileAddElement();
//            request.merchantID = GetStandardMid();
//            var result = client.ProfileAdd(request);
//            return result;
//        }
//        public ProfileResponseElement CreateRecurringProfile(ShoppingCartItemInfo recurringShoppingCartItemInfo)
//        {
//            var client = GetClient();
//            var request = MapNewRecurringProfile(recurringShoppingCartItemInfo);
//            request.merchantID = GetRecurringMid();
//            var result = client.ProfileAdd(request);
//            return result;
//        }
//        public void SaveNewProfile(ProfileResponseElement response,bool recurring=false)
//        {
//            long custRefNum = 0;
//            if (!long.TryParse(response.customerRefNum,out custRefNum))
//                throw new System.Exception("");
//            long mid = long.Parse(GetStandardMid());

//            if(recurring)
//                mid = long.Parse(GetRecurringMid());
//            var newProfile = new PaymentechProfileItem
//            {
//                CustomerID = CustomerInfo.CustomerID,
//                CustomerRefNum = custRefNum,
//                CardBrand = GetCardBrand(),
//                CardholderName = response.customerName,
//                MaskedAccountNumber = response.ccAccountNum.Substring(response.ccAccountNum.Length - 4),
//                Expiration = response.ccExp,
//                MerchantID=long.Parse(response.merchantID),
//                LastTransactionUtc=System.DateTime.UtcNow,
//                IsRecurring=recurring,
//                IsActive=true

//            };
//            PaymentechProfileProvider.InsertPaymentechProfileItem(newProfile);

//        }

//        public void SaveNewTransaction(NewOrderResponseElement response)
//        {
//            var item = new PaymentechTransactionItem
//            {
//                CustomerID = CustomerInfo.CustomerID,
//                IsSuccess = response.approvalStatus == "1",
//                MerchantID = long.Parse(response.merchantID),
//                OrderID = mOrderId,
//                PaymentProfileID = GetPaymentProfileID().Value,
//                ProcStatusMessage = response.procStatusMessage,
//                TransactionReference = response.txRefNum,
//                HostResponseCode = response.hostRespCode,
//                TransactionType = response.transType,
//                ItemOrder=0


//            };
//            PaymentechTransactionProvider.InsertCustomerTransaction(item);
//        }
        
//        public void SaveProfile(ProfileResponseElement response)
//        {
//            if(response.procStatus=="0")
//            {
//                if (response.profileAction == "CREATE")
//                    SaveNewProfile(response);
//            }
//        }
//        public NewOrderRequestElement MapNewOrderRequestElement(long customerRefNum)
//        {
//            var result = new NewOrderRequestElement();
//            var brand = GetCardBrand();
//            result.amount = GetAmount(mOrder.OrderTotalPrice);
//            result.bin = BIN;
//            result.customerRefNum = customerRefNum.ToString(CultureInfo.InvariantCulture);
//            result.industryType = "EC";
//            result.merchantID = GetStandardMid();
//            result.orderID = mOrderId.ToString(CultureInfo.InvariantCulture);
//            result.profileOrderOverideInd = "NO";
//            result.taxAmount = GetAmount(mOrder.OrderTotalTax);
//            result.taxInd = mOrder.OrderTotalTax > 0 ? "1" : "2";
//            result.terminalID = TERMINAL_ID;
//            result.transType = GetShippingRequired() ? "AC" : "A"; 
//            return result;
//        }

//        public ProfileAddElement MapNewRecurringProfile(ShoppingCartItemInfo recurringShoppingCartItemInfo)
//        {
//            var result = MapProfileAddElement();
//            var recurringBillingStartDate = GetRecurringBillingStartDate(recurringShoppingCartItemInfo);
//            var recurringBillingEndDate = GetRecurringBillingEndDate(recurringShoppingCartItemInfo);
//            var recurringAmount = GetRecurringAmount(recurringShoppingCartItemInfo);
//            result.mbRecurringStartDate = GetPaymentechDateString(recurringBillingStartDate);
//            if ((recurringBillingEndDate - recurringBillingStartDate).TotalDays < 579)
//                result.mbRecurringEndDate = GetPaymentechDateString(recurringBillingEndDate);
//            else
//                result.mbRecurringNoEndDateFlag = "Y";    
//            result.merchantID = GetRecurringMid();
//            result.mbType = "R";
//            result.mbRecurringNoEndDateFlag = "Y";
//            result.mbRecurringFrequency = GetRecurringSchedule(recurringShoppingCartItemInfo);
//            result.orderDefaultAmount = GetAmount(recurringAmount);
//            result.mbOrderIdGenerationMethod = "DI";
//            return result;
 
//        }
     
//        #endregion

//    }
//}
