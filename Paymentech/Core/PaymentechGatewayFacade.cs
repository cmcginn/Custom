using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Paymentech.PaymentechServiceReference;


namespace Paymentech.Core
{
    public class PaymentechGatewayFacade:IPaymentechGatewayFacade
    {
        private readonly PaymentechGatewaySettings _settings;
        public PaymentechGatewayFacade(PaymentechGatewaySettings settings)
        {
            _settings = settings;
        }
        #region class Methods
        public PaymentechGatewayPortTypeClient GetClient()
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            EndpointAddress endpoint = null;
            endpoint = _settings.UseSandbox ? new EndpointAddress(_settings.SandboxGatewayUrl) : new EndpointAddress(_settings.GatewayUrl);
            var result = new PaymentechGatewayPortTypeClient(binding, endpoint);
            return result;
        }
        #endregion
        #region Utilities
        /// <summary>
        /// Paymentech amount formats are strings with no decimal (i.e $10.00 = 1000)
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private string GetAmount(double amount)
        {
            return amount.ToString("0.00").Replace(".", "");
        }
        /// <summary>
        /// Paymentech date formats are strings MMddyyyy
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string GetDate(DateTime source)
        {
            var result = source.ToString("MMddyyyy");
            return result;
        }

        private string GetRecurringSchedule(RecurringCustomerProfile request)
        {
            string result = "";
            if (request.RecurringFrequency == RecurringFrequency.None)
                throw new System.ArgumentException("Recurring Frequency must be set to Yearly or Monthly");
            if (request.RecurringFrequency == RecurringFrequency.Monthly)
            {
                //billing start date must be 1 day in the future
                if (request.StartDate.Date == System.DateTime.UtcNow.Date)
                    throw new System.ArgumentException("Billing start date must be 1 day in the future");

                result = String.Format("{0} 1-12/{1} ?", ((int)request.StartDate.Day), 1);

            }
            //lifetime is included, schedule is arbirtary, max billings should be specified in request to 
            //limit 1 billing
            else if (request.RecurringFrequency == RecurringFrequency.Yearly ||
                     request.RecurringFrequency == RecurringFrequency.Lifetime)
            {
                result = String.Format("{0} {1} ?", ((int) request.StartDate.Day), request.StartDate.Month);
                return result;
            }
           
     
            return result;
        }
        private static XElement SerializeNewOrderRequestElement(NewOrderRequestElement request)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof (NewOrderRequestElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, request);
            }
            var result = XElement.Parse(builder.ToString());

            var cc = result.DescendantsAndSelf("ccAccountNum").SingleOrDefault();
            var un = result.DescendantsAndSelf("orbitalConnectionUsername").SingleOrDefault();
            var pw = result.DescendantsAndSelf("orbitalConnectionPassword").SingleOrDefault();
            var ccv = result.DescendantsAndSelf("ccCardVerifyNum").SingleOrDefault();
            if (cc != null)
                cc.Value = cc.Value.Substring(cc.Value.Length - 4);
            if (ccv != null)
                ccv.Value = "xxx";
            if (un != null)
                un.Value = "XXXXX";
            if (pw != null)
                pw.Value = "XXXXX";
            return result;
        }

        private static XElement SerializeMarkForCaptureRequest(MarkForCaptureElement request)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(MarkForCaptureElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, request);
            }
            var result = XElement.Parse(builder.ToString());

            var un = result.DescendantsAndSelf("orbitalConnectionUsername").SingleOrDefault();
            var pw = result.DescendantsAndSelf("orbitalConnectionPassword").SingleOrDefault();
         
            return result;
        }

        private static XElement SerializeReversalResponse(ReversalResponseElement response)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(ReversalResponseElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, response);
            }
            var result = XElement.Parse(builder.ToString());
            return result;
        }
        private static XElement SerializeReversalRequest(ReversalElement request)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(ReversalElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, request);
            }
            var result = XElement.Parse(builder.ToString());

            var un = result.DescendantsAndSelf("orbitalConnectionUsername").SingleOrDefault();
            var pw = result.DescendantsAndSelf("orbitalConnectionPassword").SingleOrDefault();

            return result;
        }
        private static XElement SerializeMarkForCaptureResponse(MarkForCaptureResponseElement response)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(MarkForCaptureResponseElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, response);
            }
            var result = XElement.Parse(builder.ToString());
            return result;
        }
        private static XElement SerializeNewOrderResponse(NewOrderResponseElement response)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(NewOrderResponseElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, response);
            }
            var result = XElement.Parse(builder.ToString());
            var cc = result.DescendantsAndSelf("ccAccountNum").SingleOrDefault();
            var un = result.DescendantsAndSelf("orbitalConnectionUsername").SingleOrDefault();
            var pw = result.DescendantsAndSelf("orbitalConnectionPassword").SingleOrDefault();
            var ccv = result.DescendantsAndSelf("ccCardVerifyNum").SingleOrDefault();
            if (cc != null)
                cc.Value = cc.Value.Substring(cc.Value.Length - 4);
            if (ccv != null)
                ccv.Value = "xxx";
            if (un != null)
                un.Value = "XXXXX";
            if (pw != null)
                pw.Value = "XXXXX";
            return result;
        }

        private XElement SerializeProfileAddElement(ProfileAddElement request)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(ProfileAddElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, request);
            }
            var result = XElement.Parse(builder.ToString());
            var cc = result.DescendantsAndSelf("ccAccountNum").SingleOrDefault();
            var un = result.DescendantsAndSelf("orbitalConnectionUsername").SingleOrDefault();
            var pw = result.DescendantsAndSelf("orbitalConnectionPassword").SingleOrDefault();
            var ccv = result.DescendantsAndSelf("ccCardVerifyNum").SingleOrDefault();
            if (cc != null)
                cc.Value = cc.Value.Substring(cc.Value.Length - 4);
            if (ccv != null)
                ccv.Value = "xxx";
            if (un != null)
                un.Value = "XXXXX";
            if (pw != null)
                pw.Value = "XXXXX";
            return result;
        }

        private XElement SerializeProfileResponse(ProfileResponseElement response)
        {
            var builder = new StringBuilder();
            var serializer = new XmlSerializer(typeof(ProfileResponseElement));
            using (var writer = new StringWriter(builder))
            {
                serializer.Serialize(writer, response);
            }
            var result = XElement.Parse(builder.ToString());
            var cc = result.DescendantsAndSelf("ccAccountNum").SingleOrDefault();
            var un = result.DescendantsAndSelf("orbitalConnectionUsername").SingleOrDefault();
            var pw = result.DescendantsAndSelf("orbitalConnectionPassword").SingleOrDefault();
            var ccv = result.DescendantsAndSelf("ccCardVerifyNum").SingleOrDefault();
            if (cc != null)
                cc.Value = cc.Value.Substring(cc.Value.Length - 4);
            if (ccv != null)
                ccv.Value = "xxx";
            if (un != null)
                un.Value = "XXXXX";
            if (pw != null)
                pw.Value = "XXXXX";
            return result;
        }

        private string GetCardBrand(string cardNumber)
        {
            var visa = new System.Text.RegularExpressions.Regex("^4[0-9]{12}(?:[0-9]{3})?$");
            var mc = new System.Text.RegularExpressions.Regex("^5[1-5][0-9]{14}$");
            var amex = new System.Text.RegularExpressions.Regex("^3[47][0-9]{13}$");
            var diners = new System.Text.RegularExpressions.Regex("^3(?:0[0-5]|[68][0-9])[0-9]{11}$");
            var jcb = new System.Text.RegularExpressions.Regex("^(?:2131|1800|35\\d{3})\\d{11}$");
            var discover = new System.Text.RegularExpressions.Regex("^6(?:011|5[0-9]{2})[0-9]{12}$");
            if (visa.Match(cardNumber).Success)
                return "Visa";
            if (mc.Match(cardNumber).Success)
                return "Mastercard";
            if (amex.Match(cardNumber).Success)
                return "American Express";
            if (discover.Match(cardNumber).Success)
                return "Discover";
            if (diners.Match(cardNumber).Success)
                return "Diners Club";
            if (jcb.Match(cardNumber).Success)
                return "JCB";
            return "Other";

        }
        #endregion
        #region IPaymentechGatewayFacade Implementation
        public ProfileResponse CreatePaymentechProfile(CustomerProfile customerProfile)
        {
            ProfileResponse result = new ProfileResponse();
            try
            {
                var request = MapProfileAddElement(customerProfile);
                //when creating new profile we can only use CCAccountNum not customer refnum
                request.customerRefNum = null;
                request.merchantID = _settings.MerchantId;
                var client = GetClient();
                var response = client.ProfileAdd(request);
                result = MapProfileResponseElement(response);
            }
            catch (System.Exception ex)
            {
                result = new ProfileResponse();
                result.ErrorMessage = ex.GetBaseException().Message;
            }
            return result;

        }
        public OrderResponse ProcessNewOrderPayment(OrderRequest newOrderRequest)
        {
            var result = new OrderResponse();
            try
            {
                var request = MapNewOrderRequest(newOrderRequest);
                request.merchantID = _settings.MerchantId;
                
                result.TransactionRequest = SerializeNewOrderRequestElement(request);
                var client = GetClient();
                var response = client.NewOrder(request);
                result.TransactionResponse = SerializeNewOrderResponse(response);
                result.GatewayOrderId = response.orderID;
                result.MerchantId = response.merchantID;
                
                result.ProcStatusMessage = response.procStatusMessage;
                if (response.procStatus == "0" && response.procStatusMessage.ToLower()=="approved")
                {
                    result.TransactionType = response.transType;
                    result.HostResponseCode = response.hostRespCode;
                    result.AuthorizationCode = response.authorizationCode;
                    result.TransactionRefNum = response.txRefNum;
                    if (response.transType == "AC")
                        result.PaymentStatus = PaymentStatus.AuthorizedForCapture;
                    else if (response.transType == "A")
                        result.PaymentStatus = PaymentStatus.Authorized;
                        

                }
                else
                {
                    result.PaymentStatus = PaymentStatus.Failed;
                    result.ErrorMessage = String.Format("ApprovalStatus:{0} Status Message:{1}", response.approvalStatus,response.procStatusMessage);
                }
                
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = ex.GetBaseException().Message;
            }
            return result;
        }
        public ProfileResponse CreatePaymentechRecurringProfile(RecurringCustomerProfile recurringBillingRequest)
        {
            ProfileResponse result = null;
            try
            {
                var request = MapRecurringProfileAddElement(recurringBillingRequest);
                var client = GetClient();
                var response = client.ProfileAdd(request);
                result = MapProfileResponseElement(response);
            }
            catch (System.Exception ex)
            {
                result = new ProfileResponse();
                result.ErrorMessage = ex.GetBaseException().Message;
            }
            return result;
        }

        public OrderResponse CaptureAuthPayment(PriorOrderRequest captureAuthPaymentRequest)
        {
            var result = new OrderResponse();
            try
            {
                var request = MapPriorOrderMarkForCapture(captureAuthPaymentRequest);

                var client = GetClient();
                var response = client.MarkForCapture(request);
                result.TransactionRequest = SerializeMarkForCaptureRequest(request);
                
                if (response.procStatus == "0" && response.amount == request.amount)
                {
                    result.TransactionType = "CAPTURE";
                    result.ProcStatusMessage = response.procStatusMessage;
                    result.TransactionRefNum = response.txRefNum;
                    result.TransactionResponse = SerializeMarkForCaptureResponse(response);
                    result.MerchantId = request.merchantID;
                    result.PaymentStatus = PaymentStatus.Captured;

                }
                else
                {
                    result.ErrorMessage = response.procStatusMessage;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = ex.GetBaseException().Message;
            }
            return result;
        }
        public OrderResponse Refund(PriorOrderRequest refundPaymentRequest,bool recurring=false)
        {
            var result = new OrderResponse();
            try
            {
                var request = MapNewOrderRefundRequest(refundPaymentRequest, recurring);
                var client = GetClient();
                var response = client.NewOrder(request);
                result.TransactionRequest = SerializeNewOrderRequestElement(request);
                
                if (response.procStatus == "0")
                {
                    result.TransactionType = response.transType;
                    result.TransactionRefNum = response.txRefNum;
                    result.ProcStatusMessage = response.procStatusMessage;
                    result.HostResponseCode = response.hostRespCode;
                    result.TransactionResponse = SerializeNewOrderResponse(response);
                    result.MerchantId = request.merchantID;
                    result.PaymentStatus = PaymentStatus.Captured;
                    result.GatewayOrderId = request.orderID;
                }
                else
                {
                    result.ErrorMessage = response.procStatusMessage;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = ex.GetBaseException().Message;
            }
            return result;
        }

        public OrderResponse Void(PriorOrderRequest voidPaymentRequest, bool recurring = false)
        {
            var result = new OrderResponse();
            try
            {
                var request = MapReversalElement(voidPaymentRequest, recurring);
                result.TransactionRequest = SerializeReversalRequest(request);
                var c = GetClient();
                var response = c.Reversal(request);
                if (response.procStatus == "0" && response.outstandingAmt == "0")
                {
                    result.TransactionType = "VOID";
                    result.ProcStatusMessage = response.procStatusMessage;
                    result.HostResponseCode = response.hostRespCode;
                    result.GatewayOrderId = response.orderID;
                    result.MerchantId = response.merchantID;
                    result.PaymentStatus = PaymentStatus.Voided;
                    result.TransactionRefNum = response.txRefNum;
                    result.TransactionResponse = SerializeReversalResponse(response);
                }
                else
                {
                    result.ErrorMessage = response.procStatusMessage;
                }
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = ex.GetBaseException().Message;
            }

            return result;
        }
        public ProfileResponse FetchProfile(string customerRefNum, bool recurring = false)
        {
            ProfileResponse result = null;
            var request = new ProfileFetchElement();
            request.merchantID = recurring ? _settings.RecurringMerchantId : _settings.MerchantId;
            request.customerRefNum = customerRefNum;
            request.orbitalConnectionUsername = _settings.Username;
            request.orbitalConnectionPassword = _settings.Password;
            request.bin = _settings.Bin;
            var client = GetClient();
            var response = client.ProfileFetch(request);
            result = MapProfileResponseElement(response);
            return result;
        }

        public ProfileResponse CancelProfile(string customerRefNum, bool recurring = false)
        {
            ProfileResponse result = null;
            var request = MapProfileStatusInactiveChangeElement(customerRefNum, recurring);
            var client = GetClient();
            var response = client.ProfileChange(request);
            result = MapProfileResponseElement(response);
            if(result.Success)
                result.ProfileAction = ProfileAction.Inactivate;
            
            return result;

        }

        public ProfileResponse UpdateProfile(CustomerProfile customerProfile, bool recurring = false)
        {
            ProfileResponse result = null;
            var request = MapProfileChangeElement(customerProfile,recurring);
            
            var client = GetClient();
            var response = client.ProfileChange(request);

            result = MapProfileResponseElement(response);
            

            return result;
        }
        #endregion
        #region Mapping
        #region Requests

        
        private NewOrderRequestElement MapNewOrderRequest(OrderRequestBase request,bool recurring=false)
        {
            var result = MapNewOrderRequestBase(request,recurring);
            result.transType = request.ShippingRequired ? "A" : "AC";
            return result;
        }
        private NewOrderRequestElement MapNewOrderCaptureRequest(PriorOrderRequest request)
        {
            var result = MapNewOrderRequestBase(request);
            result.transType = "FC";
            return result;
        }
        private NewOrderRequestElement MapNewOrderRefundRequest(PriorOrderRequest request,bool recurring=false)
        {
            var result = MapPriorOrderRequest(request,recurring);
            result.transType = "R";
            return result;
        }
        private NewOrderRequestElement MapPriorOrderRequest(PriorOrderRequest request,bool recurring=false)
        {
            var result = new NewOrderRequestElement();
            result.amount = GetAmount(request.TransactionTotal);
            result.bin = _settings.Bin;
            result.orbitalConnectionUsername = _settings.Username;
            result.orbitalConnectionPassword = _settings.Password;
            result.merchantID = recurring ? _settings.RecurringMerchantId : _settings.MerchantId;
            result.terminalID = _settings.TerminalId;
            result.merchantID = _settings.MerchantId;
            result.orderID = request.GatewayOrderId;
            result.customerRefNum = request.CustomerRefNum;
            result.industryType = "EC";

            result.orderID = request.GatewayOrderId;
            result.profileOrderOverideInd = "NO";
            result.taxInd = "2";
            if (request.OrderTax > 0)
            {
                result.taxAmount = GetAmount(request.OrderTax);
                result.taxInd = "1";
            }
            result.txRefNum = request.TransactionRefNum;
            result.priorAuthCd = request.AuthorizationCode;
            
            return result;
        }
        private MarkForCaptureElement MapPriorOrderMarkForCapture(PriorOrderRequest request)
        {
            var result = new MarkForCaptureElement();
            result.bin = _settings.Bin;
            result.orbitalConnectionUsername = _settings.Username;
            result.orbitalConnectionPassword = _settings.Password;
            result.terminalID = _settings.TerminalId;
            result.merchantID = _settings.MerchantId;
            result.txRefNum = request.TransactionRefNum;
            result.amount = GetAmount(request.TransactionTotal);
            result.taxInd = "2";

            if (request.OrderTax > 0)
            {
                result.taxAmount = GetAmount(request.OrderTax);
                result.taxInd = "1";
            }

            result.orderID = request.GatewayOrderId;
            return result;
        }
        private NewOrderRequestElement MapNewOrderRequestBase(OrderRequestBase request,bool recurring = false)
        {
            var result = new NewOrderRequestElement();
            result.amount = GetAmount(request.TransactionTotal);
            result.bin = _settings.Bin;
            result.orbitalConnectionUsername = _settings.Username;
            result.orbitalConnectionPassword = _settings.Password;
            result.terminalID = _settings.TerminalId;
            result.merchantID = recurring ? _settings.RecurringMerchantId : _settings.MerchantId;

            result.customerRefNum = request.CustomerRefNum;
            result.industryType = "EC";

            result.orderID = request.GatewayOrderId;
            result.profileOrderOverideInd = "NO";
            result.taxInd = "2";
            if (request.OrderTax > 0)
            {
                result.taxAmount = GetAmount(request.OrderTax);
                result.taxInd = "1";
            }
            
            return result;
        }
        private ProfileAddElement MapRecurringProfileAddElement(RecurringCustomerProfile request)
        {
            var result = MapProfileAddElement(request);
            result.merchantID = _settings.RecurringMerchantId;

            var recurringAmount = GetAmount(request.RecurringAmount);
            if (request.StartDate.Date == System.DateTime.UtcNow.Date)
                throw new System.ArgumentException("Billing start date must be 1 day in the future");
            result.mbRecurringStartDate = GetDate(request.StartDate);
            //579 days is limit otherwise, do not specify end date
            if (request.EndDate.HasValue && (request.EndDate.Value - request.StartDate).TotalDays < 579)
                result.mbRecurringEndDate = GetDate(request.EndDate.Value);
            else
                result.mbRecurringNoEndDateFlag = "Y";
            result.mbType = "R";
            result.mbRecurringNoEndDateFlag = "Y";
            result.mbRecurringFrequency = GetRecurringSchedule(request);
            if (request.RecurringFrequency == RecurringFrequency.Lifetime)
            {
                result.mbRecurringMaxBillings = "1";
                result.mbRecurringEndDate = null;
                result.mbRecurringNoEndDateFlag = null;
            }
            result.orderDefaultAmount = GetAmount(request.RecurringAmount);
            result.mbOrderIdGenerationMethod = "DI";
            return result;

        }
        private ProfileAddElement MapProfileAddElement(CustomerProfile customerProfile)
        {

            var result = new ProfileAddElement();
            result.version = "2.8";
            result.orbitalConnectionPassword = _settings.Password;
            result.orbitalConnectionUsername = _settings.Username;
            result.bin = _settings.Bin;
            result.customerProfileFromOrderInd = "NO";
            var eligibleAccountUpdaterBrands = new List<string> { "Visa", "Mastercard" };
            var brand = GetCardBrand(customerProfile.CardInfo.CardNumber);
            result.accountUpdaterEligibility = eligibleAccountUpdaterBrands.Contains(brand) ? "Y" : "N";
            result.customerProfileOrderOverideInd = "OA";
            result.customerProfileFromOrderInd = "A";
            result.customerAccountType = "CC";
            result.ccAccountNum = customerProfile.CardInfo.CardNumber;
            result.ccExp = customerProfile.CardInfo.ExpirationDate;
            result.customerEmail = customerProfile.EmailAddress;
            result.customerName = customerProfile.CardInfo.CardholderName;
            if (customerProfile.BillingAddressInfo != null)
            {
                result.customerAddress1 = customerProfile.BillingAddressInfo.Address1;
                //Chase likes address 2 to be 1 if its present just .... Because
                if (!String.IsNullOrEmpty(customerProfile.BillingAddressInfo.Address2))
                {
                    result.customerAddress1 = customerProfile.BillingAddressInfo.Address2;
                    result.customerAddress2 = customerProfile.BillingAddressInfo.Address1;
                }
                result.customerCity = customerProfile.BillingAddressInfo.City;
                result.customerCountryCode = customerProfile.BillingAddressInfo.Country;
                result.customerState = customerProfile.BillingAddressInfo.StateProvince;
                result.customerPhone = customerProfile.BillingAddressInfo.PhoneNumber;
                result.customerZIP = customerProfile.BillingAddressInfo.PostalCode;
                result.customerName = customerProfile.BillingAddressInfo.BillingAddressName;

            }
            return result;

        }
        private ProfileChangeElement MapProfileStatusInactiveChangeElement(string customerRefNum, bool recurring = false)
        {
            var result = new ProfileChangeElement();
            result.version = "2.8";
            result.orbitalConnectionPassword = _settings.Password;
            result.orbitalConnectionUsername = _settings.Username;
            result.merchantID = recurring ? _settings.RecurringMerchantId : _settings.MerchantId;
            result.bin = _settings.Bin;
            result.customerRefNum = customerRefNum;
            result.status = "I";
            return result;

        }
        private ProfileChangeElement MapProfileChangeElement(CustomerProfile customerProfile, bool recurring = false)
        {
            var result = new ProfileChangeElement();
            result.version = "2.8";
            result.orbitalConnectionPassword = _settings.Password;
            result.orbitalConnectionUsername = _settings.Username;
            result.merchantID = recurring ? _settings.RecurringMerchantId : _settings.MerchantId;
            result.bin = _settings.Bin;
            var eligibleAccountUpdaterBrands = new List<string> { "visa", "mastercard" };
            var brand = customerProfile.CardInfo.CardBrand;
            result.accountUpdaterEligibility = eligibleAccountUpdaterBrands.Contains(brand) ? "Y" : "N";
            result.customerProfileOrderOverideInd = "OA";

            result.customerAccountType = "CC";
            result.customerRefNum = customerProfile.CustomerRefNum;
            result.ccExp = customerProfile.CardInfo.ExpirationDate;
            result.customerEmail = customerProfile.EmailAddress;
            result.customerName = customerProfile.CardInfo.CardholderName;
            if (customerProfile.BillingAddressInfo != null)
            {
                result.customerAddress1 = customerProfile.BillingAddressInfo.Address1;
                //Chase likes address 2 to be 1 if its present just .... Because
                if (!String.IsNullOrEmpty(customerProfile.BillingAddressInfo.Address2))
                {
                    result.customerAddress1 = customerProfile.BillingAddressInfo.Address2;
                    result.customerAddress2 = customerProfile.BillingAddressInfo.Address1;
                }
                result.customerCity = customerProfile.BillingAddressInfo.City;
                result.customerCountryCode = customerProfile.BillingAddressInfo.Country;
                result.customerState = customerProfile.BillingAddressInfo.StateProvince;
                result.customerPhone = customerProfile.BillingAddressInfo.PhoneNumber;
                result.customerZIP = customerProfile.BillingAddressInfo.PostalCode;


            }
            return result;
        }
        private ReversalElement MapReversalElement(PriorOrderRequest request,bool recurring=false)
        {
            var result = new ReversalElement();
            result.version = "2.8";
            result.orbitalConnectionPassword = _settings.Password;
            result.orbitalConnectionUsername = _settings.Username;
            result.bin = _settings.Bin;
            result.terminalID = _settings.TerminalId;
            result.txRefNum = request.TransactionRefNum;
            result.orderID = request.GatewayOrderId;
            result.merchantID = recurring ? _settings.RecurringMerchantId : _settings.MerchantId;
            return result;
        }
        #endregion
        #region Responses

        private ProfileResponse MapProfileResponseElement(ProfileResponseElement response)
        {
            var result = new ProfileResponse();
            result.MerchantId = response.merchantID;
            result.CardInfo = new CardInfo();
            result.CardInfo.CardNumber = response.ccAccountNum;
            result.CardInfo.MaskedCardNumber = response.ccAccountNum.Substring(response.ccAccountNum.Length - 4);
            result.CardInfo.CardholderName = response.customerName;
            result.CardInfo.ExpirationDate = response.ccExp;
            result.CardInfo.CardBrand = GetCardBrand(response.ccAccountNum);
            result.EmailAddress = response.customerEmail;
            result.BillingAddressInfo = new BillingAddressInfo();
            result.BillingAddressInfo.Address1 = response.customerAddress1;
            result.BillingAddressInfo.Address2 = response.customerAddress2;
            result.BillingAddressInfo.City = response.customerCity;
            result.BillingAddressInfo.Country = response.customerCountryCode;
            result.BillingAddressInfo.PhoneNumber = response.customerPhone;
            result.MerchantId = response.merchantID;
            result.BillingAddressInfo.StateProvince = response.customerState;
            result.BillingAddressInfo.PostalCode = response.customerZIP;
            result.CustomerRefNum = response.customerRefNum;
            if (response.procStatus != "0")
            {
                result = new ProfileResponse();
                result.ErrorMessage = response.procStatusMessage;
            }
            switch (response.profileAction)
            {
                case "CREATE":
                    result.ProfileAction = ProfileAction.Create;
                    break;
                case "READ":
                    result.ProfileAction = ProfileAction.Fetch;
                    break;
                case "UPDATE":
                    result.ProfileAction = ProfileAction.Update;
                    return result;
                default:
                    break;
            }
            return result;
        }
        #endregion
        #endregion
    }
}
