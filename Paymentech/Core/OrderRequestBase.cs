using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public abstract class OrderRequestBase:OrderBase
    {
        public virtual double TransactionTotal { get; set; }
        public virtual double OrderShipping { get; set; }
        public virtual double OrderTax { get; set; }
        public virtual bool ShippingRequired { get; set; }

    }
}
