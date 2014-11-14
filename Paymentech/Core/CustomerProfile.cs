using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public class CustomerProfile
    {
        public virtual string MerchantId { get; set; }
        public virtual string CustomerRefNum { get; set; }
        public virtual string EmailAddress { get; set; }
        public virtual CardInfo CardInfo { get; set; }
        public virtual BillingAddressInfo BillingAddressInfo { get; set; }
    }
}
