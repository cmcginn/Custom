using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Ecommerce;
using CMS.EcommerceProvider;
using Paymentech.Tests.Mocks;
using Paymentech.Tests.PaymentechGateway;
using System.ServiceModel;
using CMS.Globalization;
using CMS.DataEngine;
using CMSCustom.revolutiongolf.ecommerce.paymentech.Providers;

namespace Paymentech.Tests.Gateway
{
    public class PaymentechGatewayProvider : CMSPaymentGatewayProvider
    {
        public const string BIN = "000001";
        public const string TERMINAL_ID = "001";
        public const string VERSION = "2.8";

        #region Testablity helpers

        public OrderInfo TestOrderInfo
        {
            get { return this.mOrder; }
            set { this.mOrder = value; }
        }
        #endregion

        public override void ProcessPayment()
        {
            base.ProcessPayment();

        }
        public void ProcessNewOrderResponse(NewOrderResponseElement newOrderResponse)
        {
            var orderStatues = OrderStatusInfoProvider.GetOrderStatuses();

            //if its not zero, it failed
            if (newOrderResponse.procStatus == "0")
            {

                //authorized and captured, nothing left to do
                if (newOrderResponse.transType == "AC")
                    mOrder.OrderStatusID = orderStatues.Single(x => x.StatusName == "Completed").StatusID;
                //authorization only, need to capture this at a later time
                else if (newOrderResponse.transType == "A")
                {
                    //TODO: add pending capture status
                    mOrder.OrderStatusID = orderStatues.Single(x => x.StatusName == "InProgress").StatusID;
                    //need to save capture info
                }
            }
        }
        public void ProcessPaymentForOrder()
        {
            var profiles = GetCustomerPaymentProfiles();
            PaymentechProfile profile = null;
            //customer has selected payment profile
            if(GetPaymentProfileID().HasValue)
            {
                profile = profiles.Single(x => x.ItemId == GetPaymentProfileID().Value);
            }else
            {
                //no existing profile selected, customer entered details, check if we have a profile for customer, to avoid duplication
                profile = GetCustomerPaymentProfile(GetCardNumber(), GetCardExpiration());
            }
            //TODO:Update card num possibly same card number different expiration
            //we have a new profile
            if(profile==null)
            {
                var response = CreateProfile();
                SaveNewProfile(response);
                profiles = GetCustomerPaymentProfiles();
                profile = profiles.Single();
            }

            var p = MapNewOrderRequestElement(profile.CustomerRefNum);
            p.orbitalConnectionPassword = GetClientPassword();
            p.orbitalConnectionUsername = GetClientUsername();

            var c = GetClient();
            var newOrderResponse = c.NewOrder(p);
            ProcessNewOrderResponse(newOrderResponse);
        }

        #region Utilities
        public List<PaymentechProfile> GetCustomerPaymentProfiles()
        {
            var result = PaymentechProfileProvider.GetCustomerPaymentProfiles(CustomerInfo);
            return result;
        }
        public PaymentechProfile GetCustomerPaymentProfile(string accountNumber, string expiration)
        {           
            PaymentechProfile result = null;
            if(!String.IsNullOrEmpty(accountNumber))
            {
                accountNumber = accountNumber.Substring(accountNumber.Length,4);
                var profiles = GetCustomerPaymentProfiles();
                result = profiles.Where(x => x.MaskedAccountNumber == accountNumber && x.Expiration == expiration).OrderByDescending(x => x.LastTransactionUtc).FirstOrDefault();
            }
            return result;
        }
        PaymentechProfileProvider _paymentechProfileProvider;
        PaymentechProfileProvider PaymentechProfileProvider
        {
            get { return _paymentechProfileProvider ?? (_paymentechProfileProvider = new PaymentechProfileProvider()); }
        }
        CustomerInfo _customerInfo;
        CustomerInfo CustomerInfo
        {
            get { return _customerInfo ?? (_customerInfo = CustomerInfoProvider.GetCustomerInfo(mOrder.OrderCustomerID)); }
        }
        //CustomerInfo GetCustomerInfo()
        //{
        //    return CustomerInfoProvider.GetCustomerInfo(mOrder.OrderCustomerID);
        //}
        public string GetRecurringMid()
        {
            return "239880";
        }
        public string GetStandardMid()
        {
            return "239877";
        }
        public string GetClientUsername()
        {
            return "TMCGINN5353";
        }


        public string GetClientPassword()
        {
            return "1X1T54HBRG3";
        }

        public string GetPrimaryServiceUrl()
        {
            return "https://wsvar.paymentech.net/PaymentechGateway";
        }
        public string GetSecondaryServiceUrl()
        {
            return "https://wsvar2.paymentech.net/PaymentechGateway";
        }
        public int? GetPaymentProfileID()
        {
            return 4;
        }
        public string GetCardNumber()
        {
            return "4112344112344113";
        }
        public string GetCardBrand()
        {
            return "visa";
        }
        /// <summary>
        /// Paymentech amount formats are strings with no decimal (i.e $10.00 = 1000)
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public string GetAmount(double amount)
        {
            return amount.ToString(CultureInfo.InvariantCulture).Replace(".", "");
        }
        public string GetCardExpiration()
        {
            return "201807";
        }
        public string GetCardCcv()
        {
            return "411";
        }
        public PaymentechGatewayPortTypeClient GetClient()
        {
            //var result = new PaymentechGatewayPortTypeClient()
           // result.Url = GetPrimaryServiceUrl();
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            EndpointAddress endpoint = new EndpointAddress(GetPrimaryServiceUrl());
            var result = new PaymentechGatewayPortTypeClient(binding, endpoint);
            return result;
        }
        public string GetNewCustomerRefNum()
        {
            var rand = new Random();
            return rand.Next(1000000, 9999999).ToString();
        }

        ObjectQuery<OrderItemInfo> _orderItemInfos;
        public ObjectQuery<OrderItemInfo> OrderItemInfos
        {
            get
            {
                return _orderItemInfos ?? (_orderItemInfos = OrderItemInfoProvider.GetOrderItems(mOrderId));
            }
        }


        public bool GetShippingRequired()
        {
            var result = false;
            result = OrderItemInfos.Any(x => x.OrderItemSKU.SKUNeedsShipping);
            return result;
        }
        public string GetStateCode(int stateId)
        {
            string result = null;
            var stateInfo = StateInfoProvider.GetStateInfo(stateId);
            if (stateInfo != null)
                result = stateInfo.StateCode;
            return result;
        }
        public string GetCountryCode(int countryId)
        {
            string result = null;
            var countryInfo = CountryInfoProvider.GetCountryInfo(countryId); 
            if (countryInfo != null)
                result = countryInfo.CountryTwoLetterCode;
            return result;
        }
        public ProfileAddElement MapProfileAddElement()
        {
            
            var result = new ProfileAddElement();
            result.version = "2.8";
            result.orbitalConnectionPassword = GetClientPassword();
            result.orbitalConnectionUsername = GetClientUsername();
            result.bin = BIN;            
            result.ccAccountNum = GetCardNumber();
            result.ccExp = GetCardExpiration();
            result.customerEmail = CustomerInfo.CustomerEmail;
            result.customerAddress1 = mOrder.OrderBillingAddress.AddressLine1;
            result.customerAddress2 = mOrder.OrderBillingAddress.AddressLine2;
            result.customerCity = mOrder.OrderBillingAddress.AddressCity;
            result.customerCountryCode = GetCountryCode(mOrder.OrderBillingAddress.AddressCountryID);
            result.customerState = GetStateCode(mOrder.OrderBillingAddress.AddressStateID);
            result.customerPhone = mOrder.OrderBillingAddress.AddressPhone;
            result.customerZIP = mOrder.OrderBillingAddress.AddressZip;
            result.customerProfileFromOrderInd = "NO";
            result.customerRefNum = GetNewCustomerRefNum();
            var eligibleAccountUpdaterBrands = new List<string> {"visa", "mastercard"};
            var brand = GetCardBrand();
            result.accountUpdaterEligibility = eligibleAccountUpdaterBrands.Contains(brand) ? "Y" : "N";
            result.customerProfileOrderOverideInd = "OA";
            result.customerProfileFromOrderInd = "A";
            result.customerAccountType="CC";
            return result;
            
        }
        public ProfileResponseElement CreateProfile()
        {
            var client = GetClient();
            var request = MapProfileAddElement();
            request.merchantID = GetStandardMid();
            var result = client.ProfileAdd(request);
            return result;
        }
        public void SaveNewProfile(ProfileResponseElement response,bool recurring=false)
        {
            long custRefNum = 0;
            if (!long.TryParse(response.customerRefNum,out custRefNum))
                throw new System.Exception("");
            long mid = long.Parse(GetStandardMid());

            if(recurring)
                mid = long.Parse(GetRecurringMid());

            PaymentechProfileProvider.InsertPaymentProfile(CustomerInfo, custRefNum, mid, GetCardBrand(), response.customerName, response.ccAccountNum, response.ccExp, System.DateTime.UtcNow, false, true);
        }
        public void SaveProfile(ProfileResponseElement response)
        {
            if(response.procStatus=="0")
            {
                if (response.profileAction == "CREATE")
                    SaveNewProfile(response);
            }
        }
        public NewOrderRequestElement MapNewOrderRequestElement(long customerRefNum)
        {
            var result = new NewOrderRequestElement();
            var brand = GetCardBrand();

            
            result.amount = GetAmount(mOrder.OrderTotalPrice);
          
            result.bin = BIN;



            result.customerRefNum = customerRefNum.ToString(CultureInfo.InvariantCulture);
            result.industryType = "EC";
            result.merchantID = GetStandardMid();
            result.orderID = mOrderId.ToString(CultureInfo.InvariantCulture);
            result.profileOrderOverideInd = "NO";
            result.taxAmount = GetAmount(mOrder.OrderTotalTax);
            result.taxInd = mOrder.OrderTotalTax > 0 ? "1" : "2";
            result.terminalID = TERMINAL_ID;
            result.transType = GetShippingRequired() ? "AC" : "A"; 
            return result;

            

        }

        //public void MapNewProfileInfo(NewOrderRequestElement request)
        //{
        //    result.avsAddress1 = mOrder.OrderBillingAddress.AddressLine1;
        //    result.avsAddress2 = mOrder.OrderBillingAddress.AddressLine2;
        //    result.avsCity = mOrder.OrderBillingAddress.AddressCity;
        //    result.avsCountryCode = GetCountryCode(mOrder.OrderBillingAddress.AddressCountryID);
        //    result.avsState = GetStateCode(mOrder.OrderBillingAddress.AddressStateID);
        //    result.avsZip = mOrder.OrderBillingAddress.AddressZip;
        //    result.avsName = mOrder.OrderBillingAddress.AddressPersonalName;
        //    result.avsPhone = mOrder.OrderBillingAddress.AddressPhone;
        //    result.ccAccountNum = GetCardNumber();
        //    result.ccCardVerifyNum = GetCardCcv();
        //    result.ccExp = GetCardExpiration();
        //    result.customerEmail = CustomerInfo.CustomerEmail;
        //    result.customerName = mOrder.OrderBillingAddress.AddressPersonalName;
        //    result.customerEmail = CustomerInfo.CustomerEmail;
        //    result.customerPhone = mOrder.OrderBillingAddress.AddressPhone;
            

        //}
        #endregion

    }
}
