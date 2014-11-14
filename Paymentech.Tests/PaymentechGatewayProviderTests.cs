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
using PaymentechGateway.Provider;


namespace Paymentech.Tests
{
    [TestClass]
    public class PaymentechGatewayProviderTests
    {
        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            CustomTableItemGenerator.RegisterCustomTable<PaymentechProfileItem>(PaymentechProfileItem.CLASS_NAME);
            CustomTableItemGenerator.RegisterCustomTable<PaymentechTransactionItem>(PaymentechTransactionItem.CLASS_NAME);
        }
        public static TestablePaymentechGatewayProvider GetTarget()
        {
            return new TestablePaymentechGatewayProvider();
        }
        public OrderInfo GetWellknownOrderInfo()
        {

            return OrderInfoProvider.GetOrderInfo(264);
            // var result = TestHelper.CreateOrder()
        }
        public OrderInfo CreateNewOrderInfo()
        {
            var custInfo = GetWellknownCustomerInfo();
            var orderInfo = GetWellknownOrderInfo();
            OrderAddressInfoProvider.GetAddressInfo(484);
            //var address = AddressInfoProvider.GetAddressInfo(orderInfo.OrderBillingAddress.AddressGUID);
            var result = TestHelper.CreateOrder(custInfo.CustomerID, 484, 10.00d, 0, 0);
            return result;
        }
        public CustomerInfo GetWellknownCustomerInfo()
        {
            return CustomerInfoProvider.GetCustomerInfo(78);
        }
        PaymentechProfileItem GetWellKnownPaymentechProfileItem()
        {
            var provider = new PaymentechProfileProvider();
            var profiles = provider.GetCustomerPaymentProfiles(GetWellknownCustomerInfo());
            var result = profiles.Single(x => x.ItemID == 12);
            return result;
        }

        [TestMethod]
        public void NewOrderSetup()
        {
            var username = TestHelper.RandomString(8);
            var email = String.Format("{0}@donotresolve.com", username);
            var customer = TestHelper.CreateRegisteredCustomer(username, email);
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
            Assert.IsFalse(beforeProfile.ItemID == afterProfile.ItemID, "7");


        }

        [TestMethod]
        public void FetchCustomerProfileTest()
        {
            var target = GetTarget();
            var paymentechProfile = GetWellKnownPaymentechProfileItem();
            var actual = target.FetchCustomerProfileAccessor(paymentechProfile);
        }

        [TestMethod]
        public void GetCustomerPaymentProfilesTest()
        {
            var customerInfo = GetWellknownCustomerInfo();
            var orderInfo = GetWellknownOrderInfo();

            var target = GetTarget();
            var actual = target.GetCustomerPaymentProfilesAccessor(customerInfo);
            Assert.IsTrue(actual.Any());
        }

        [TestMethod]
        public void CreateRecurringCustomerProfileTest()
        {
            var target = GetTarget();
            //var result = new RecurringCustomerProfile();
            var customerInfo = GetWellknownCustomerInfo();
            var orderInfo = CreateNewOrderInfo();
            OrderItemInfo newItem = new OrderItemInfo
            {
                OrderItemSKUName = "Recurring Subscription",
                OrderItemOrderID = orderInfo.OrderID,
                OrderItemSKUID = 142,
                OrderItemUnitPrice = 39.99,
                OrderItemUnitCount = 1
            };

            // Create the order item
            OrderItemInfoProvider.SetOrderItemInfo(newItem);
            target.RecurringItems.Add(newItem);
            target.Order = orderInfo;
            var profile = target.MapProfileRecurringInfoAccessor(customerInfo, orderInfo, newItem);
            var actual = target.CreateRecurringCustomerProfileAccessor(profile,newItem);
            Assert.IsTrue(actual.Success);
        }
        [TestMethod]
        public void CreateNewOrderTest()
        {
            var customer = GetWellknownCustomerInfo();
            var transactionProvider = new PaymentechTransactionProvider();
            var existingTransactions = transactionProvider.GetCustomerTransactions(customer);
            int transactionCount = existingTransactions.Count;
            var order = CreateNewOrderInfo();
            var profile = GetWellKnownPaymentechProfileItem();
            var target = GetTarget();
            var actual = target.CreateNewOrderAccessor(customer, order, profile);
            var updatedTransactions = transactionProvider.GetCustomerTransactions(customer);
            var lastTransaction = updatedTransactions.OrderBy(x => x.ItemCreatedWhen).ToList().Last();
            Assert.IsTrue(actual.Success, "1");
            Assert.IsTrue(updatedTransactions.Count - 1 == transactionCount, "2");
            Assert.IsTrue(lastTransaction.TransactionType == "AC");
            Assert.IsTrue(lastTransaction.ProcStatusMessage == "Approved");
        }

        [TestMethod]
        public void VoidOrderTest()
        {
            
            var customer = GetWellknownCustomerInfo();
            var order = CreateNewOrderInfo();
            var profile = GetWellKnownPaymentechProfileItem();
            var target = GetTarget();
            var orderResponse = target.CreateNewOrderAccessor(customer, order, profile);
            target.VoidOrderTransactionAccessor(customer, order);
            var transactionProvider = new PaymentechTransactionProvider();
            var transactions = transactionProvider.GetOrderTransactionsForOrder(order);
            var orderTxn = transactions.FirstOrDefault(x=>x.TransactionType=="A" || x.TransactionType=="AC");
            var voidTransaction = transactions.OrderBy(x => x.ItemCreatedWhen).Last();
            Assert.IsTrue(voidTransaction.TransactionType == "VOID","1");
            Assert.IsTrue(voidTransaction.TransactionReference == orderTxn.TransactionReference, "2");
            Assert.IsTrue(voidTransaction.GatewayOrderID == orderTxn.GatewayOrderID, "3");

        }

        [TestMethod]
        public void RefundOrderTest()
        {

            var customer = GetWellknownCustomerInfo();
            var order = CreateNewOrderInfo();
            var profile = GetWellKnownPaymentechProfileItem();
            var target = GetTarget();
            var orderResponse = target.CreateNewOrderAccessor(customer, order, profile);
            target.RefundOrderTransactionAccessor(customer, order,1.00d);
            var transactionProvider = new PaymentechTransactionProvider();
            var transactions = transactionProvider.GetOrderTransactionsForOrder(order);
            var orderTxn = transactions.FirstOrDefault(x => x.TransactionType == "A" || x.TransactionType == "AC");
            var voidTransaction = transactions.OrderBy(x => x.ItemCreatedWhen).Last();
            Assert.IsTrue(voidTransaction.TransactionType == "FR", "1");
            Assert.IsTrue(voidTransaction.GatewayOrderID == orderTxn.GatewayOrderID, "2");

        }

        #region Process Payment Tests
        [TestMethod]
        public void ProcessPaymentTest_WhenSingle_RecurringItem()
        {
            var target = GetTarget();
            var order = CreateNewOrderInfo();

            OrderItemInfo newItem = new OrderItemInfo
            {
                OrderItemSKUName = "Recurring Subscription",
                OrderItemOrderID = order.OrderID,
                OrderItemSKUID = 142,
                OrderItemUnitPrice = 39.99,
                OrderItemUnitCount = 1
            };

            // Create the order item
            OrderItemInfoProvider.SetOrderItemInfo(newItem);
            target.RecurringItems.Add(newItem);
            var orderItems = OrderItemInfoProvider.GetOrderItems(order.OrderID);
            Assert.IsTrue(orderItems.Any());
            target.Order = order;
            target.ProcessPayment();
            
        }
        #endregion
    }
}
