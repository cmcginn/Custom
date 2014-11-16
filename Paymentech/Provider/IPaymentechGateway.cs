using CMS.CustomTables.Types;
using CMS.Ecommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Provider
{
    public interface IPaymentechGateway
    {
        void CapturePayment(OrderInfo orderInfo);
        void CapturePayment(int orderId);
        void RefundOrder(OrderInfo orderInfo, double? amount = null);
        void RefundOrder(int orderId, double? amount = null);
        void ProcessScheduledRecurringPayment(OrderInfo orderInfo, int initialOrderId);
        void ProcessScheduledRecurringPayment(int orderId, int initialOrderId);
        void CancelRecurringProfile(OrderItemInfo initialOrderItem);
        void VoidOrder(int orderId);
        void VoidOrder(OrderInfo orderInfo);
        void UpdateProfile(PaymentechProfileItem profile, IAddress billingAddress, string cardholderName, int expirationMonth, int expirationYear);
    }
}
