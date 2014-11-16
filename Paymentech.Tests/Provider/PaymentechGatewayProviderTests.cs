using System;
using System.Linq;
using CMS.Base;
using CMS.CustomTables;
using CMS.CustomTables.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CMS.Globalization;
using CMS.DataProviderSQL;
using CMS.Ecommerce;
using Paymentech.Data;
using Paymentech.Tests.Helpers;
using Paymentech.Tests.Provider;
using System.Collections.Generic;



namespace Paymentech.Tests.Provider
{
    [TestClass]
    public class PaymentechGatewayProviderTests : PaymentechTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            TestInitializer.Initialize();
        }
        public TestablePaymentechGatewayProvider GetTarget()
        {
            return new TestablePaymentechGatewayProvider();
        }
        #region Sync Tests
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

            Assert.IsTrue(actual.CardInfo.CardBrand == "Visa");
        }
        [TestMethod]
        public void CreateNewOrderTest()
        {
            var customer = GetWellknownCustomerInfo();
            var transactionProvider = new PaymentechTransactionProvider();
            var existingTransactions = transactionProvider.GetCustomerTransactions(customer);
            int transactionCount = existingTransactions.Count;
            var order = CreateNewOrderInfo();
            var newItem = GetShippingRequiredOrderItem();
            newItem.OrderItemOrderID = order.OrderID;

            // Create the order item
            OrderItemInfoProvider.SetOrderItemInfo(newItem);

            var profile = GetWellKnownPaymentechProfileItem();
            var target = GetTarget();
            target.OrderItems.Add(newItem);
            var actual = target.CreateNewOrderAccessor(customer, order, profile);
            var updatedTransactions = transactionProvider.GetCustomerTransactions(customer);
            var lastTransaction = updatedTransactions.OrderBy(x => x.ItemCreatedWhen).ToList().Last();
            Assert.IsTrue(actual.Success, "1");
            Assert.IsTrue(updatedTransactions.Count - 1 == transactionCount, "2");
            Assert.IsTrue(lastTransaction.TransactionType == "A");
            Assert.IsTrue(lastTransaction.ProcStatusMessage == "Approved");
        }

     
        #endregion
        #region Process Payment Tests
        [TestMethod]
        public void ProcessPaymentTest_WhenSingle_RecurringItem()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateStandardTestSetup(setup);
            target.RecurringItems.Add(setup.OrderItems.First());

            target.Order = setup.OrderInfo;
            target.ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("Completed", "ECommerceSite");
            Assert.IsTrue(setup.NewProfile != null, "Expected new profile");
            Assert.IsTrue(setup.NewProfile.RecurringOrderItemID == setup.OrderItems.Single().OrderItemID,"Expected RecurringOrderItemId to match OrderItemID");
            Assert.IsTrue(setup.NewProfile.MerchantID == 239880, "Expected Recurring MerchantId");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Completed Order Status");
            Assert.IsTrue(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be true");

  
        }
        [TestMethod]
        public void ProcessPaymentTest_WhenSingleItem_NewProfile_ShippingRequired()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateShippingTestSetup(setup);
            target.OrderItems.Add(setup.OrderItems.First());
            target.Order = setup.OrderInfo;
            target.ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PendingCapture", "ECommerceSite");
            ValidateNonRecurringOrder(setup.OrderInfo.OrderID);
            Assert.IsTrue(setup.NewProfile != null, "Expected new profile");          
            Assert.IsTrue(setup.NewProfile.MerchantID == 239877, "Expected Standard MerchantId");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");

        }

        [TestMethod]
        public void ProcessPaymentTest_WhenSingleItem_ExistingProfile_ShippingRequired()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateShippingTestSetup(setup);
            target.OrderItems.Add(setup.OrderItems.First());
            target.Order = setup.OrderInfo;
            target.SelectedProfileId = setup.ExistingProfiles.Where(x => x.MerchantID == 239877).Last().ItemID;
            target.ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PendingCapture", "ECommerceSite");
            ValidateNonRecurringOrder(setup.OrderInfo.OrderID);
            var txn = setup.TransactionItems.OrderBy(x=>x.ItemCreatedWhen).Last();
            Assert.IsNull(setup.NewProfile,"No new profile expected");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");
            Assert.AreEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Equal order and Transaction Amounts");

        }
        [TestMethod]
        public void ProcessPaymentTest_WhenSingleItem_ExistingProfile_NoShippingRequired()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateStandardTestSetup(setup);
            target.OrderItems.Add(setup.OrderItems.First());
            target.Order = setup.OrderInfo;
            target.SelectedProfileId = setup.ExistingProfiles.Where(x => x.MerchantID == 239877).Last().ItemID;
            target.ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("Completed", "ECommerceSite");
            var txn = setup.TransactionItems.OrderBy(x => x.ItemCreatedWhen).Last();
            ValidateNonRecurringOrder(setup.OrderInfo.OrderID);
            Assert.IsNull(setup.NewProfile, "No new profile expected");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Completed Order Status");
            Assert.IsTrue(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be true");
            Assert.AreEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Equal order and Transaction Amounts");
        }
        [TestMethod]
        public void ProcessPaymentTest_Shipping_And_NonShipping_Items_Existing_Profile()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateMixedShippingNoShippingTestSetup(setup);
            target.OrderItems.AddRange(setup.OrderItems);
            target.Order = setup.OrderInfo;
            target.SelectedProfileId = setup.ExistingProfiles.Where(x => x.MerchantID == 239877).Last().ItemID;
            target.ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PendingCapture", "ECommerceSite");
            ValidateNonRecurringOrder(setup.OrderInfo.OrderID);
            var txn = setup.TransactionItems.OrderBy(x => x.ItemCreatedWhen).Last();
            Assert.IsNull(setup.NewProfile, "No new profile expected");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");
            Assert.AreEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Equal order and Transaction Amounts");
        }

        [TestMethod]
        public void ProcessPaymentTest_Shipping_And_NonShippingRecurring_Items_Existing_Profile()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateMixedShippingNoShippingTestSetup(setup);
            target.RecurringItems.Add(setup.OrderItems.ElementAt(1));
            target.OrderItems.AddRange(setup.OrderItems);
            target.Order = setup.OrderInfo;
            target.SelectedProfileId = setup.ExistingProfiles.Where(x => x.MerchantID == 239877).Last().ItemID;
            target.ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PendingCapture", "ECommerceSite");
            ValidateNonRecurringOrder(setup.OrderInfo.OrderID);
            var txn = setup.TransactionItems.OrderBy(x => x.ItemCreatedWhen).Last();
            Assert.IsNotNull(setup.NewProfile, "New profile expected");
            Assert.IsTrue(setup.NewProfile.MerchantID == 239880, "Expected new Recurring Profile");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");
            Assert.AreNotEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Not Equal order and Transaction Amounts Due to Recurring Amount in seperate transaction");
        }
        #endregion

        #region IPaymentechGateway Tests
     
        #endregion

        #region Exception Tests
        #endregion




    }
}
