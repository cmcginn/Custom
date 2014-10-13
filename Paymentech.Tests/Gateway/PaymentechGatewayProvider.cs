using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Ecommerce;
using CMS.EcommerceProvider;
using Paymentech.Tests.Mocks;
using Paymentech.Tests.PaymentechGateway;
using System.ServiceModel;
using CMS.Globalization;

namespace Paymentech.Tests.Gateway
{
    public class PaymentechGatewayProvider : CMSPaymentGatewayProvider
    {
        public const string BIN = "000001";
        public const string TERMINAL_ID = "001";

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

        #region Utilities

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
            return "vufRa2ef";
        }

        public string GetPrimaryServiceUrl()
        {
            return "https://wsvar.paymentech.net/PaymentechGateway";
        }
        public string GetSecondaryServiceUrl()
        {
            return "https://wsvar2.paymentech.net/PaymentechGateway";
        }
        public string GetCardNumber()
        {
            return "4112344112344113";
        }
        public string GetCardBrand()
        {
            return "VISA";
        }

        public string GetCardExpiration()
        {
            return "01012018";
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
        public ProfileAddElement MapProfileAddElement()
        {
            
            var result = new ProfileAddElement();
            result.orbitalConnectionPassword = GetClientPassword();
            result.orbitalConnectionUsername = GetClientUsername();
            result.bin = BIN;            
            result.ccAccountNum = GetCardNumber();
            result.ccExp = GetCardExpiration();
            result.customerEmail = CustomerInfo.CustomerEmail;
            result.customerAddress1 = mOrder.OrderBillingAddress.AddressLine1;
            result.customerAddress2 = mOrder.OrderBillingAddress.AddressLine2;
            result.customerCity = mOrder.OrderBillingAddress.AddressCity;
            result.customerCountryCode = CountryInfoProvider.GetCountryInfo(mOrder.OrderBillingAddress.AddressCountryID).CountryTwoLetterCode;
            var stateInfo = StateInfoProvider.GetStateInfo(mOrder.OrderBillingAddress.AddressStateID);
            if (stateInfo != null)
                result.customerState = stateInfo.StateCode;
            result.customerPhone = mOrder.OrderBillingAddress.AddressPhone;
            result.customerZIP = mOrder.OrderBillingAddress.AddressZip;
            result.customerProfileFromOrderInd = "NO";
            result.customerRefNum = GetNewCustomerRefNum();
            return result;
            
        }
       
        #endregion

    }
}
