using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CMS.CustomTables;
using CMS.CustomTables.Types;
using Paymentech.Provider;
using Paymentech.Tests.Provider;
using Paymentech.Data;
using CMS.Ecommerce;
using System.Linq;
using System.Collections.Generic;
using Paymentech.Tests.Helpers;
namespace Paymentech.Tests
{
    [TestClass]
    public class IPaymentechGatewayTests:PaymentechTestBase
    {
        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            CustomTableItemGenerator.RegisterCustomTable<PaymentechProfileItem>(PaymentechProfileItem.CLASS_NAME);
            CustomTableItemGenerator.RegisterCustomTable<PaymentechTransactionItem>(PaymentechTransactionItem.CLASS_NAME);
        }

        IPaymentechGateway GetTarget()
        {
            return new TestablePaymentechGatewayProvider();
        }

        [TestMethod]
        public void VoidOrderTest()
        {

            var target = GetTarget();
            //create the order to capture
            var setup = new PaymentTestSetup();
            CreateShippingTestSetup(setup);
            ((TestablePaymentechGatewayProvider)target).OrderItems.Add(setup.OrderItems.First());
            ((TestablePaymentechGatewayProvider)target).Order = setup.OrderInfo;
            ((TestablePaymentechGatewayProvider)target).ProcessPayment();
            setup.PostProcess();
            target.VoidOrder(setup.OrderInfo);
            setup.PostProcess();
            var txn = setup.TransactionItems.OrderBy(x => x.ItemCreatedWhen).Last();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("Voided", "ECommerceSite");
            Assert.AreEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Equal Transaction and Order Amounts");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Voided Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected Order Is Paid = False");
            Assert.IsTrue(txn.TransactionType == "VOID", "Expected Force Refund transaction");

        }
        [TestMethod]
        public void RefundOrderTest()
        {
            var target = GetTarget();
            //create the order to capture
            var setup = new PaymentTestSetup();
            CreateShippingTestSetup(setup);
            ((TestablePaymentechGatewayProvider)target).OrderItems.Add(setup.OrderItems.First());
            ((TestablePaymentechGatewayProvider)target).Order = setup.OrderInfo;
            ((TestablePaymentechGatewayProvider)target).ProcessPayment();
            setup.PostProcess();
            target.RefundOrder(setup.OrderInfo);
            setup.PostProcess();
            var txn = setup.TransactionItems.OrderBy(x => x.ItemCreatedWhen).Last();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("Refunded", "ECommerceSite");
            Assert.AreEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Equal Transaction and Order Amounts");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Refunded Order Status");
            Assert.IsFalse(setup.OrderInfo.OrderIsPaid, "Expected Order Is Paid = False");
            Assert.IsTrue(txn.TransactionType == "FR", "Expected Force Refund transaction");
        }

        [TestMethod]
        public void CapturePaymentTest()
        {
            var target = GetTarget();
            //create the order to capture
            var setup = new PaymentTestSetup();
            CreateShippingTestSetup(setup);
            ((TestablePaymentechGatewayProvider)target).OrderItems.Add(setup.OrderItems.First());
            ((TestablePaymentechGatewayProvider)target).Order = setup.OrderInfo;
            ((TestablePaymentechGatewayProvider)target).ProcessPayment();
            setup.PostProcess();
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PendingCapture", "ECommerceSite");
            var receivedStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PaymentReceived", "ECommerceSite");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Pending Capture Order Status");
            target.CapturePayment(setup.OrderInfo);
            ValidateNonRecurringOrder(((TestablePaymentechGatewayProvider)target).Order.OrderID);
            setup.PostProcess();
            var txn = setup.TransactionItems.OrderBy(x => x.ItemCreatedWhen).Last();
            Assert.IsTrue(txn.TransactionType == "CAPTURE", "Expected capture transaction");
            Assert.IsFalse(String.IsNullOrEmpty(txn.TransactionReference), "Transaction Reference should not be null");
            Assert.IsTrue(setup.OrderInfo.OrderStatusID == receivedStatus.StatusID, "Expected Payment Received Status Id");
            Assert.IsTrue(setup.OrderInfo.OrderIsPaid, "Expected Order Is Paid = True");
            Assert.AreEqual(txn.TransactionAmount, setup.OrderInfo.OrderTotalPrice, "Expected Equal Transaction and Order Amounts");


        }
        [TestMethod]
        public void ProcessScheduledRecurringPaymentTest()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateStandardTestSetup(setup);
            ((TestablePaymentechGatewayProvider)target).RecurringItems.Add(setup.OrderItems.First());
            ((TestablePaymentechGatewayProvider)target).Order = setup.OrderInfo;
            ((TestablePaymentechGatewayProvider)target).ProcessPayment();
            setup.PostProcess();
            var recurringSetup = new PaymentTestSetup();
            CreateStandardTestSetup(recurringSetup);
            target.ProcessScheduledRecurringPayment(recurringSetup.OrderInfo, setup.OrderInfo.OrderID);
            var orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("PaymentReceived", "ECommerceSite");
            Assert.IsTrue(recurringSetup.OrderInfo.OrderStatusID == orderStatus.StatusID, "Expected Payment Received Order Status");
            Assert.IsTrue(recurringSetup.OrderInfo.OrderIsPaid, "Expected OrderIsPaid to be true");
        }
        [TestMethod]
        public void CancelRecurringProfileTest()
        {
            var target = GetTarget();
            var setup = new PaymentTestSetup();
            CreateStandardTestSetup(setup);
            ((TestablePaymentechGatewayProvider)target).RecurringItems.Add(setup.OrderItems.First());
            ((TestablePaymentechGatewayProvider)target).Order = setup.OrderInfo;
            ((TestablePaymentechGatewayProvider)target).ProcessPayment();
            setup.PostProcess();
            //cancel profile
            target.CancelRecurringProfile(setup.OrderItems.First());
            var profileProvider = new PaymentechProfileProvider();
            var profiles = profileProvider.GetCustomerPaymentProfiles(setup.CustomerInfo);
            var expectedProfile = profiles.OrderBy(x => x.ItemCreatedWhen).Last();
            Assert.IsFalse(expectedProfile.IsActive, "Profile should not be active");


        }

        [TestMethod]
        public void UpdateProfileTest()
        {
            var target = GetTarget();
            //create the order to capture
            var setup = new PaymentTestSetup();
            CreateShippingTestSetup(setup);
            ((TestablePaymentechGatewayProvider)target).OrderItems.Add(setup.OrderItems.First());
            ((TestablePaymentechGatewayProvider)target).Order = setup.OrderInfo;
            ((TestablePaymentechGatewayProvider)target).ProcessPayment();
            setup.PostProcess();
            var profile = setup.ProfileProvider.GetCustomerPaymentProfiles(setup.CustomerInfo).OrderBy(x => x.ItemCreatedWhen).Last();
            var address = TestHelper.CreateBillingAddress(setup.CustomerInfo.CustomerID);
            address.AddressCity = "Toledo";
            target.UpdateProfile(profile, address, "Tony Danza", 7, 2017);
            var updatedProfile = setup.ProfileProvider.GetCustomerPaymentProfiles(setup.CustomerInfo).Single(x => x.ItemID == profile.ItemID);
            Assert.IsTrue(updatedProfile.CardholderName == "Tony Danza");
        }
    }
}
