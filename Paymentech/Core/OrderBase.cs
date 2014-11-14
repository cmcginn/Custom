using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public abstract class OrderBase
    {

        public virtual string CustomerRefNum { get; set; }
        public virtual string GatewayOrderId { get; set; }
    }
}
