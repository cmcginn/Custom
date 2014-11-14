using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public enum PaymentStatus
    {
        None=0,
        Authorized=1,
        AuthorizedForCapture=2,
        Captured,
        Failed=3,
        Voided=4
    }
}
