using System;
using System.Linq;
using CMS.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CMS.Globalization;
using CMS.DataProviderSQL;
using CMS.Ecommerce;
using Paymentech.Tests.Gateway;
using Paymentech.Tests.Helpers;
using CMSCustom.revolutiongolf.ecommerce.paymentech.Providers;

namespace Paymentech.Tests
{
    [TestClass]
    public class PaymentechGatewayProviderTests
    {
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
            Assert.IsNotNull(o);

            Console.Out.Write(String.Format("Customer ID {0}", customer.CustomerID));
            Console.Out.Write(String.Format("Address {0}", address.AddressID));
            Console.Out.Write(String.Format("Order {0}", o.OrderID));


        }
        [TestMethod]
        public void CreateProfileTest()
        {
            var target = new PaymentechGatewayProvider();
            target.TestOrderInfo = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
            var actual = target.CreateProfile();
            target.SaveProfile(actual);
        }
        [TestMethod]
        public void GetProfileTest()
        {
            var target = new PaymentechGatewayProvider();
            target.TestOrderInfo = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
            var actual = target.GetCustomerPaymentProfiles();
            Assert.IsTrue(actual.Any());
        }

        [TestMethod]
        public void ProcessPaymentTest()
        {
            var target = new PaymentechGatewayProvider();
            target.TestOrderInfo = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
            target.ProcessPaymentForOrder();
            Assert.IsTrue(target.TestOrderInfo.OrderStatusID == 3);
        }

        //[TestMethod]
        //public void MapNewOrderRequestTest()
        //{
        //    var target = new PaymentechGatewayProvider();
        //    target.TestOrderInfo = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
        //    var actual = target.MapNewOrderRequestElement();
        //    Assert.IsNotNull(actual);
        //}

        //[TestMethod]
        //public void ProcessPaymentForOrderTest()
        //{
        //    var target = new PaymentechGatewayProvider();
        //    target.TestOrderInfo = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
        //    target.ProcessPaymentForOrder();
        //}
        //[TestMethod]
        //public void InsertCustomerProfile()
        //{
        //    var target = new PaymentechProfileProvider();
        //    var c = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
        //    target.InsertPaymentProfile(c.OrderCustomerID, 11111111);
        //}
    }
}
