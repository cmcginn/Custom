using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Paymentech.Core;

namespace PaymentechGateway.Tests
{
    [TestClass]
    public class PaymentechGatewayFacadeTests
    {
        public static string WellKnownRecurringProfileId = "50239060";
        IPaymentechGatewayFacade GetTarget()
        {
            var settings = new PaymentechGatewaySettings();

            settings.Bin = "000001";
            settings.TerminalId = "001";
            settings.Username = ConfigurationManager.AppSettings["username"];
            settings.Password = ConfigurationManager.AppSettings["password"];
            settings.SandboxGatewayUrl = "https://wsvar.paymentech.net/PaymentechGateway";
            settings.SandboxGatewayFailoverUrl = "https://wsvar2.paymentech.net/PaymentechGateway";
            settings.MerchantId = ConfigurationManager.AppSettings["merchantid"];
            settings.RecurringMerchantId = ConfigurationManager.AppSettings["recurringmerchantid"];
            settings.UseSandbox = true;
            return new PaymentechGatewayFacade(settings);
        }

        OrderRequest GetNewOrderRequest()
        {
            var result = new OrderRequest();
            result.CustomerRefNum = "50593080";
            result.GatewayOrderId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 22);
            result.OrderShipping = 5.00d;
            result.TransactionTotal = 100.20d;
            result.OrderTax = 3.50d;
            result.ShippingRequired = true;
            return result;
        }
        CardInfo GetCardInfo()
        {
            var result = new CardInfo
            {

                CardholderName = "Good RGCustomer",
                CardNumber = "6559906559906557",
                CCV = "613",
                ExpirationDate = "102015"

            };
            return result;
        }
        BillingAddressInfo GetBillingAddressInfo()
        {
            BillingAddressInfo result = new BillingAddressInfo
            {

                Address1 = "1 Northeastern Blvd",
                Address2 = "Apt 2",
                City = "Bedford",
                StateProvince = "NH",
                Country = "US",
                PostalCode = "03109-1234",
                PhoneNumber = "888-555-5555"

            };
            return result;
        }

        RecurringCustomerProfile GetRecurringBillingRequest()
        {
            var result = new RecurringCustomerProfile();
            result.BillingAddressInfo = GetBillingAddressInfo();
            result.CardInfo = GetCardInfo();
            result.RecurringAmount = 10.00d;
            result.StartDate = System.DateTime.UtcNow.AddDays(1);
            result.RecurringFrequency = RecurringFrequency.Monthly;
            return result;
        }

        #region Common Test Methods

        ProfileResponse CreatePaymentechProfile()
        {
            var target = GetTarget();
            var cp = new CustomerProfile { EmailAddress = "GoodRGCustomer@donotresolve.com" };
            cp.CardInfo = GetCardInfo();
            cp.BillingAddressInfo = GetBillingAddressInfo();
            var result = target.CreatePaymentechProfile(cp);

            return result;
        }
        #endregion
        #region Create Payment Profile Tests
        [TestMethod]
        public void CreatePaymentechProfileTest()
        {

            var actual = CreatePaymentechProfile();
            Assert.IsTrue(actual.Success);
            Assert.IsTrue(long.Parse(actual.CustomerRefNum) > 0, "Expected Long Customer Ref Num > 0");
            Assert.IsTrue(actual.ProfileAction == ProfileAction.Create, "Expected Profile type Create");
            Assert.IsTrue(actual.MerchantId == ConfigurationManager.AppSettings["merchantid"],
                "Expected Standard Merchant Id");
        }
        #endregion

        #region IPaymentechFacade CreatePaymentechProfile Tests
        [TestMethod]
        public void ProcessNewOrderPaymentTest()
        {
            var target = GetTarget();
            var newOrder = GetNewOrderRequest();
            var actual = target.ProcessNewOrderPayment(newOrder);
            Assert.IsTrue(actual.Success);
        }
        #endregion
        [TestMethod]
        public void CreatePaymentechRecurringProfileTest()
        {
            var target = GetTarget();
            var request = GetRecurringBillingRequest();
            var actual = target.CreatePaymentechRecurringProfile(request);
            Assert.IsTrue(long.Parse(actual.CustomerRefNum) > 0);
            Assert.IsTrue(actual.Success);
        }

        [TestMethod]
        public void CaptureAuthPaymentTest()
        {
            var target = GetTarget();
            var profile = CreatePaymentechProfile();
            var authOrder = GetNewOrderRequest();
            authOrder.CustomerRefNum = profile.CustomerRefNum;
            authOrder.ShippingRequired = true;
            var authResponse = target.ProcessNewOrderPayment(authOrder);
            var request = new PriorOrderRequest();
            request.CustomerRefNum = authOrder.CustomerRefNum;
            request.TransactionRefNum = authResponse.TransactionRefNum;
            request.TransactionTotal = authOrder.TransactionTotal;
            request.OrderTax = authOrder.OrderTax;
            request.AuthorizationCode = authResponse.AuthorizationCode;
            request.GatewayOrderId = authOrder.GatewayOrderId;
            target.CaptureAuthPayment(request);

        }

        [TestMethod]
        public void RefundTest()
        {
            var target = GetTarget();
            var profile = CreatePaymentechProfile();
            var refundOrder = GetNewOrderRequest();
            refundOrder.CustomerRefNum = profile.CustomerRefNum;
            var refundResponse = target.ProcessNewOrderPayment(refundOrder);
            var request = new PriorOrderRequest();
            request.CustomerRefNum = refundOrder.CustomerRefNum;
            request.TransactionRefNum = refundResponse.TransactionRefNum;
            request.TransactionTotal = refundOrder.TransactionTotal;
            request.OrderTax = refundOrder.OrderTax;
            request.AuthorizationCode = refundResponse.AuthorizationCode;
            request.GatewayOrderId = refundOrder.GatewayOrderId;
            target.Refund(request);
        }

        [TestMethod]
        public void FetchProfileTest()
        {
            var target = GetTarget();
            var profile = CreatePaymentechProfile();
            var actual = target.FetchProfile(profile.CustomerRefNum, false);
            Assert.IsTrue(actual.ProfileAction == ProfileAction.Fetch, "Expected Profile Action Fetch");
            Assert.IsTrue(actual.MerchantId == profile.MerchantId, "Expected MerchantId Match");
            Assert.IsTrue(actual.CustomerRefNum == profile.CustomerRefNum, "Expected Customer Ref Num Match");
        }

        [TestMethod]
        public void UpdateProfileTest()
        {
            var target = GetTarget();
            var profile = CreatePaymentechProfile();
            profile.CardInfo.CardholderName = "CHANGED RG CUST";
            target.UpdateProfile(profile);
            var actual = target.FetchProfile(profile.CustomerRefNum, false);
            Assert.IsTrue(actual.CardInfo.CardholderName == profile.CardInfo.CardholderName, "Expected Cardholder Name Match");

        }

        [TestMethod]
        public void CancelProfileTest()
        {
            var profile = CreatePaymentechProfile();
            var target = GetTarget();
            var actual = target.CancelProfile(profile.CustomerRefNum, false);
            Assert.IsTrue(actual.Success);
            Assert.IsTrue(actual.ProfileAction == ProfileAction.Inactivate);
        }

        [TestMethod]
        public void VoidTest()
        {
            var target = GetTarget();
            var newOrder = GetNewOrderRequest();
            var order = target.ProcessNewOrderPayment(newOrder);
            var request = new PriorOrderRequest { TransactionRefNum = order.TransactionRefNum, GatewayOrderId = order.GatewayOrderId };
            var actual = target.Void(request);

        }
        #region Conditional Check Tests
        [TestMethod]
        public void ProcessNewOrderPaymentTest_Check_AuthorizationCode_Exists()
        {
            var target = GetTarget();
            var newOrder = GetNewOrderRequest();
            var actual = target.ProcessNewOrderPayment(newOrder);
            Assert.IsTrue(!String.IsNullOrEmpty(actual.AuthorizationCode), "!String.IsNullOrEmpty(actual.AuthorizationCode)");
            Assert.IsTrue(actual.TransactionRequest != null, "actual.TransactionRequest != null");
            Assert.IsTrue(actual.TransactionResponse != null, "actual.TransactionResponse != null");
            Assert.IsFalse(string.IsNullOrEmpty(actual.TransactionRefNum),
                "string.IsNullOrEmpty(actual.TransactionRefNum)");
            Assert.IsTrue(actual.Success, "actual.Success");
        }
        [TestMethod]
        public void ProcessNewOrderPaymentTest_WhenShippingRequired_CheckPaymentStatus_Authorized()
        {
            var target = GetTarget();
            var newOrder = GetNewOrderRequest();
            newOrder.ShippingRequired = true;
            var actual = target.ProcessNewOrderPayment(newOrder);
            Assert.IsTrue(actual.PaymentStatus == PaymentStatus.Authorized);
        }
        [TestMethod]
        public void ProcessNewOrderPaymentTest_WhenShippingNotRequired_CheckPaymentStatus_AuthorizedForCapture()
        {
            var target = GetTarget();
            var newOrder = GetNewOrderRequest();
            newOrder.ShippingRequired = false;
            var actual = target.ProcessNewOrderPayment(newOrder);
            Assert.IsTrue(actual.PaymentStatus == PaymentStatus.AuthorizedForCapture);
        }

        [TestMethod]
        public void CreatePaymentechProfileTest_When_LifetimeMembership_CheckMaxBilling()
        {
            //validated on payment gateway
            var target = GetTarget();
            var request = GetRecurringBillingRequest();
            request.RecurringFrequency = RecurringFrequency.Lifetime;
            request.RecurringAmount = 100.80d;
            var actual = target.CreatePaymentechRecurringProfile(request);
            Assert.IsTrue(long.Parse(actual.CustomerRefNum) > 0);
            Assert.IsTrue(actual.Success);
        }

        [TestMethod]
        public void FetchProfileTest_When_Recurring_Check_MerchantId()
        {
            var target = GetTarget();
            var actual = target.FetchProfile(WellKnownRecurringProfileId, true);
            Assert.IsTrue(actual.MerchantId == ConfigurationManager.AppSettings["recurringmerchantid"]);
        }

        [TestMethod]
        public void CaptureAuthPaymentTest_Check_PaymentStatus_Captured()
        {
            var target = GetTarget();
            var profile = CreatePaymentechProfile();
            var authOrder = GetNewOrderRequest();
            authOrder.CustomerRefNum = profile.CustomerRefNum;
            authOrder.ShippingRequired = true;
            var authResponse = target.ProcessNewOrderPayment(authOrder);
            var request = new PriorOrderRequest();
            request.CustomerRefNum = authOrder.CustomerRefNum;
            request.TransactionRefNum = authResponse.TransactionRefNum;
            request.TransactionTotal = authOrder.TransactionTotal;
            request.OrderTax = authOrder.OrderTax;
            request.AuthorizationCode = authResponse.AuthorizationCode;
            request.GatewayOrderId = authOrder.GatewayOrderId;
            var actual = target.CaptureAuthPayment(request);
            Assert.IsTrue(actual.PaymentStatus == PaymentStatus.Captured);
            //target.CaptureAuthPayment()
        }
        #endregion
    }
}
