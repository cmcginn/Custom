using CMS.CustomTables;
using CMS.CustomTables.Types;
using CMS.Ecommerce;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Paymentech.Data;
using Paymentech.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Tests
{
    public abstract class PaymentechTestBase
    {

        #region Setup Helpers
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
        public PaymentechProfileItem GetWellKnownPaymentechProfileItem()
        {
            var provider = new PaymentechProfileProvider();
            var profiles = provider.GetCustomerPaymentProfiles(GetWellknownCustomerInfo());
            var result = profiles.Single(x => x.ItemID == 12);
            return result;
        }
        #endregion
        #region utility
        public void ValidateNonRecurringOrder(int orderId)
        {
            var lastOrder = OrderInfoProvider.GetOrderInfo(orderId);
            var tp = new PaymentechTransactionProvider();
            var transactions = tp.GetOrderTransactionsForOrder(lastOrder);
            var gatewayId = lastOrder.OrderGUID.ToString().Replace("-", "").Substring(0, 22);
            Assert.IsTrue(transactions.Any(), "No transactions found for Order");

            transactions.ForEach(x =>
            {
                Assert.IsTrue(x.PaymentProfileID > 0, "Expected Payment Profile ID");
                Assert.IsFalse(String.IsNullOrEmpty(x.TransactionRequest), "Transaction Request should not be empty");
                Assert.IsFalse(String.IsNullOrEmpty(x.TransactionResponse), "Transaction Response should not be empty");
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
