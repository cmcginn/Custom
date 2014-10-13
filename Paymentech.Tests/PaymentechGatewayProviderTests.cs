using System;
using System.Linq;
using CMS.Base;
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
        public void ProcessPaymentTest()
        {
            var target = new PaymentechGatewayProvider();
            target.TestOrderInfo = OrderInfoProvider.GetOrderInfo(TestHelper.GetWellKnownOrderId());
            var actual = target.MapProfileAddElement();
            Assert.IsNotNull(actual);
            //target.ProcessPayment();
        }

    }
}
