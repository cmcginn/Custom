using System;
using System.Linq;
using CMS.Base;
using CMS.CustomTables;
using CMS.CustomTables.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CMS.Globalization;
using CMS.DataProviderSQL;
using CMS.Ecommerce;
using Paymentech.Tests.Gateway;
using Paymentech.Tests.Helpers;


namespace Paymentech.Tests
{
    [TestClass]
    public class PaymentechGatewayProviderTests
    {
        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            CustomTableItemGenerator.RegisterCustomTable<PaymentechProfileItem>(PaymentechProfileItem.CLASS_NAME);
        }
        public static TestablePaymentechGatewayProvider GetTarget()
        {
            return new TestablePaymentechGatewayProvider();
        }
        public OrderInfo GetWellknownOrderInfo()
        {
            return OrderInfoProvider.GetOrderInfo(256);
        }

        public CustomerInfo GetWellknownCustomerInfo()
        {
            return CustomerInfoProvider.GetCustomerInfo(78);
        }
        [TestMethod]
        public void NewOrderSetup()
        {
            var username = TestHelper.RandomString(8);
            var email = String.Format("{0}@donotresolve.com", username);
            var customer = TestHelper.CreateRegisteredCustomer(username,email);
            Assert.IsNotNull(customer);

            var address = TestHelper.CreateBillingAddress(customer.CustomerID);
            Assert.IsNotNull(address);

            var o = TestHelper.CreateOrder(customer.CustomerID, address.AddressID, 100, 7, 10);
            var sc = TestHelper.CreateShoppingCartInfo(o);
            Assert.IsNotNull(o);

            Console.Out.Write(String.Format("Customer ID {0}", customer.CustomerID));
            Console.Out.Write(String.Format("Address {0}", address.AddressID));
            Console.Out.Write(String.Format("Order {0}", o.OrderID));


        }
       
        [TestMethod]
        public void MapCustomerProfileTest()
        {
            var target = GetTarget();
            var actual = target.MapCustomerProfileAccessor(GetWellknownCustomerInfo(), GetWellknownOrderInfo());

        }

        [TestMethod]
        public void CreateCustomerProfileTest()
        {
            var target = GetTarget();
            target.Order = GetWellknownOrderInfo();
            var pp = new PaymentechProfileProvider();
            var beforeProfile = pp.GetCustomerPaymentProfiles(GetWellknownCustomerInfo()).OrderBy(x => x.LastTransactionUtc).ToList().Last();
            var profile = target.MapCustomerProfileAccessor(GetWellknownCustomerInfo(), GetWellknownOrderInfo());
            var actual = target.CreateCustomerProfileAccessor(profile);
            Assert.IsFalse(String.IsNullOrEmpty(actual.CardInfo.MaskedCardNumber), "1");
            Assert.IsFalse(String.IsNullOrEmpty(actual.CustomerRefNum), "2");
            Assert.IsFalse(String.IsNullOrEmpty(actual.MerchantId), "3");
            Assert.IsFalse(String.IsNullOrEmpty(actual.CardInfo.ExpirationDate), "4");
            Assert.IsFalse(String.IsNullOrEmpty(actual.CardInfo.CardholderName), "5");
            Assert.IsFalse(String.IsNullOrEmpty(actual.CardInfo.CardBrand), "6");
            
            var afterProfile = pp.GetCustomerPaymentProfiles(GetWellknownCustomerInfo()).OrderBy(x => x.LastTransactionUtc).ToList().Last();
            Assert.IsFalse(beforeProfile.ItemID == afterProfile.ItemID,"7");


        }
    }
}
