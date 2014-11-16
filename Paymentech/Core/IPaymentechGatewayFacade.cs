using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public interface IPaymentechGatewayFacade
    {
        ProfileResponse CreatePaymentechProfile(CustomerProfile customerProfile);
        OrderResponse ProcessNewOrderPayment(OrderRequest newOrderRequest);
        ProfileResponse CreatePaymentechRecurringProfile(RecurringCustomerProfile recurringBillingRequest);
        OrderResponse CaptureAuthPayment(PriorOrderRequest captureAuthPaymentRequest);
        OrderResponse Refund(PriorOrderRequest refundPaymentRequest,bool recurring=false);
        OrderResponse Void(PriorOrderRequest voidPaymentRequest, bool recurring = false);
        ProfileResponse FetchProfile(string customerRefNum, bool recurring = false);
        ProfileResponse CancelProfile(string customerRefNum, bool recurring = false);
        ProfileResponse UpdateProfile(CustomerProfile customerProfile, bool recurring = false);       
    }
}
