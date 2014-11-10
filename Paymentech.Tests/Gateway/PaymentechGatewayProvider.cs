using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.EcommerceProvider;
using PaymentechGateway.Provider;
using CMS.Ecommerce;
using CMS.Globalization;
using CMS.CustomTables.Types;

namespace Paymentech.Tests.Gateway
{
    public class PaymentechGatewayProvider: CMSPaymentGatewayProvider
    {

        public override void ProcessPayment()
        {
            base.ProcessPayment();
        }
        #region Class Methods
        protected virtual void InsertPaymentechProfileItem(PaymentechProfileItem paymentechProfile)
        {
            var profileProvider = new PaymentechProfileProvider();
            profileProvider.InsertPaymentechProfileItem(paymentechProfile);
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
        protected virtual ProfileResponse CreateCustomerProfile(CustomerProfile profile)
        {
            var facade = new PaymentechGatewayFacade(PaymentechGatewaySettings);
            var result = facade.CreatePaymentechProfile(profile);
            if(result.Success)
            {
          
                    profile.BillingAddressInfo = result.BillingAddressInfo;
                    profile.CardInfo = result.CardInfo;
                    profile.MerchantId = result.MerchantId;
                    profile.CustomerRefNum = result.CustomerRefNum;
                    profile.EmailAddress = result.EmailAddress;
                    
               
                var pp = MapPaymentechProfileItem(profile, GetOrderInfo().OrderCustomerID);
                InsertPaymentechProfileItem(pp);
            }
            return result;
        }
        protected virtual CustomerProfile MapCustomerProfile(CustomerInfo customerInfo, OrderInfo orderInfo)
        {

            var result = new CustomerProfile();
            
            result.CardInfo = MapCustomerCardInfo();
            result.EmailAddress = customerInfo.CustomerEmail;
            
            if(orderInfo.OrderBillingAddress != null)
            {
                result.BillingAddressInfo = new BillingAddressInfo();
                result.BillingAddressInfo.Address1 = orderInfo.OrderBillingAddress.AddressLine1;
                result.BillingAddressInfo.Address2 = orderInfo.OrderBillingAddress.AddressLine2;
                result.BillingAddressInfo.City = orderInfo.OrderBillingAddress.AddressCity;
                var countryInfo = CountryInfoProvider.GetCountryInfo(orderInfo.OrderBillingAddress.AddressCountryID);
                if(countryInfo != null)                
                    result.BillingAddressInfo.Country = countryInfo.CountryTwoLetterCode;
                
                var stateProvince = StateInfoProvider.GetStateInfo(orderInfo.OrderBillingAddress.AddressStateID);
                if (stateProvince != null)
                    result.BillingAddressInfo.StateProvince = stateProvince.StateCode;
                result.BillingAddressInfo.PhoneNumber = orderInfo.OrderBillingAddress.AddressPhone;
                result.BillingAddressInfo.PostalCode = orderInfo.OrderBillingAddress.AddressZip;
                result.BillingAddressInfo.BillingAddressName = orderInfo.OrderBillingAddress.AddressPersonalName;
                
                

            }
            return result;
        }

        protected virtual PaymentechProfileItem MapPaymentechProfileItem(CustomerProfile profile, int customerID)
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
            result.IsRecurring = profile.MerchantId == _paymentechGatewaySettings.RecurringMerchantId;
            return result;
        }
        #endregion

        #region Class Properties
        PaymentechGatewaySettings _paymentechGatewaySettings;
        protected virtual PaymentechGatewaySettings PaymentechGatewaySettings
        {
            get
            {
                if(_paymentechGatewaySettings == null)
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
                        SandboxGatewayUrl = "https://wsvar2.paymentech.net/PaymentechGateway",
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

        //protected virtual PaymentechGatewaySettings GetSettings()
        //{
        //    var result
        //}
        #endregion
    }
}
