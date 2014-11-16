using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Provider
{
    public class PaymentechProviderException:Exception
    {
        public PaymentechProviderException(string message):base(message)
        {
            
        }
        public PaymentechProviderException(string message, Exception innerException) : base(message, innerException) { }
    }
}
