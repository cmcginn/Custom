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
        public int GetWellKnownStandardProfileId()
        {
            return 22;
        }
        public OrderItemInfo GetOrderItemInfoBySkuID(int skuId)
        {
            var skuInfo = SKUInfoProvider.GetSKUInfo(skuId);
            OrderItemInfo newItem = new OrderItemInfo
            {
                OrderItemSKUName = skuInfo.SKUName,              
                OrderItemSKUID = skuInfo.SKUID,
                OrderItemUnitPrice = skuInfo.SKUPrice,
                OrderItemUnitCount = 1,


            };
            newItem.OrderItemSKU = skuInfo;
            return newItem;
        }
        public OrderItemInfo GetShippingRequiredOrderItem()
        {
            return GetOrderItemInfoBySkuID(142);
        }
        public OrderItemInfo GetNoShippingRequiredOrderItem()
        {
            return GetOrderItemInfoBySkuID(182);
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
            Assert.IsNull(setup.NewProfile,"No new profile expected");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");

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
            ValidateNonRecurringOrder(setup.OrderInfo.OrderID);
            Assert.IsNull(setup.NewProfile, "No new profile expected");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Completed Order Status");
            Assert.IsTrue(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be true");
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
            Assert.IsNull(setup.NewProfile, "No new profile expected");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");

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
            Assert.IsNotNull(setup.NewProfile, "New profile expected");
            Assert.IsTrue(setup.NewProfile.MerchantID == 239880, "Expected new Recurring Profile");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be false");

        }
        #endregion

      


        #region utility
        public void ValidateNonRecurringOrder(int orderId)
        {
            var lastOrder = OrderInfoProvider.GetOrderInfo(orderId);
            var tp = new PaymentechTransactionProvider();
            var transactions = tp.GetOrderTransactionsForOrder(lastOrder);
            var gatewayId = lastOrder.OrderGUID.ToString().Replace("-","").Substring(0,22);
            Assert.IsTrue(transactions.Any(),"No transactions found for Order");
            
            transactions.ForEach(x=>{
                Assert.IsTrue(x.PaymentProfileID > 0, "Expected Payment Profile ID");
                Assert.IsFalse(String.IsNullOrEmpty(x.TransactionRequest),"Transaction Request should not be empty");
                Assert.IsFalse(String.IsNullOrEmpty(x.TransactionResponse),"Transaction Response should not be empty");
                Assert.IsTrue(x.GatewayOrderID == gatewayId, "Expected Gateway Order ID");

            });
            //
           // Assert.IsFalse(String.IsNullOrEmpty(transactions.First().TransactionResponse),"Transaction Response should not be empty");
           
        }
        public void CreateStandardTestSetup(PaymentTestSetup setup)
        {
            setup.OrderInfo = CreateNewOrderInfo();
            setup.CustomerInfo = CustomerInfoProvider.GetCustomerInfo(setup.OrderInfo.OrderCustomerID);
            setup.ProfileProvider = new PaymentechProfileProvider();
            setup.ExistingProfiles = setup.ProfileProvider.GetCustomerPaymentProfiles(setup.CustomerInfo);
            
            if (setup.ExistingProfiles.Any())
                setup.LastProfileID = setup.ExistingProfiles.Max(x => x.ItemID);
            
            var newItem = GetNoShippingRequiredOrderItem();
            newItem.OrderItemOrderID = setup.OrderInfo.OrderID;
            // Create the order item
            OrderItemInfoProvider.SetOrderItemInfo(newItem);
            
            setup.OrderItems = new List<OrderItemInfo> { newItem };
            setup.OrderInfo.OrderTotalPrice = setup.OrderItems.Select(x => x.OrderItemUnitPrice * x.OrderItemUnitCount).Sum();
        }
        public void CreateShippingTestSetup(PaymentTestSetup setup)
        {
            setup.OrderInfo = CreateNewOrderInfo();
            setup.CustomerInfo = CustomerInfoProvider.GetCustomerInfo(setup.OrderInfo.OrderCustomerID);
            setup.ProfileProvider = new PaymentechProfileProvider();
            setup.ExistingProfiles = setup.ProfileProvider.GetCustomerPaymentProfiles(setup.CustomerInfo);
            if (setup.ExistingProfiles.Any())
                setup.LastProfileID = setup.ExistingProfiles.Max(x => x.ItemID);

            var newItem = GetShippingRequiredOrderItem();
            newItem.OrderItemOrderID = setup.OrderInfo.OrderID;
            // Create the order item
            OrderItemInfoProvider.SetOrderItemInfo(newItem);
            
            setup.OrderItems = new List<OrderItemInfo> { newItem };
            setup.OrderInfo.OrderTotalPrice = setup.OrderItems.Select(x => x.OrderItemUnitPrice * x.OrderItemUnitCount).Sum();
        }
        public void CreateMixedShippingNoShippingTestSetup(PaymentTestSetup setup)
        {
            setup.OrderInfo = CreateNewOrderInfo();
            setup.CustomerInfo = CustomerInfoProvider.GetCustomerInfo(setup.OrderInfo.OrderCustomerID);
            setup.ProfileProvider = new PaymentechProfileProvider();
            setup.ExistingProfiles = setup.ProfileProvider.GetCustomerPaymentProfiles(setup.CustomerInfo);
            if (setup.ExistingProfiles.Any())
                setup.LastProfileID = setup.ExistingProfiles.Max(x => x.ItemID);

            var shippingItem = GetShippingRequiredOrderItem();
            shippingItem.OrderItemOrderID = setup.OrderInfo.OrderID;

            var standardItem = GetNoShippingRequiredOrderItem();
            standardItem.OrderItemOrderID = setup.OrderInfo.OrderID;
            // Create the order item
            OrderItemInfoProvider.SetOrderItemInfo(shippingItem);
            OrderItemInfoProvider.SetOrderItemInfo(standardItem);
            
            setup.OrderItems = new List<OrderItemInfo> { shippingItem, standardItem };
            setup.OrderInfo.OrderTotalPrice = setup.OrderItems.Select(x => x.OrderItemUnitPrice * x.OrderItemUnitCount).Sum();

        }
        public class PaymentTestSetup
        {
            public List<OrderItemInfo> OrderItems { get; set; }
            public List<PaymentechProfileItem> ExistingProfiles { get; set; }

            public List<PaymentechTransactionItem> TransactionItems { get; set; }
            public int LastProfileID { get; set; }
            public PaymentechProfileItem NewProfile { get; set; }    
            public CustomerInfo CustomerInfo { get; set; }
            public OrderInfo OrderInfo { get; set; }

            public PaymentechProfileProvider ProfileProvider { get; set; }

            
            public void PostProcess()
            {
                NewProfile = ProfileProvider.GetCustomerPaymentProfiles(CustomerInfo).SingleOrDefault(x => x.ItemID > LastProfileID);
                var tp = new PaymentechTransactionProvider();
                TransactionItems = tp.GetOrderTransactionsForOrder(OrderInfo);
               
            }

            
        }

        
        #endregion
    }
}
