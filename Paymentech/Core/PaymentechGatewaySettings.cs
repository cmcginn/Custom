using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public class PaymentechGatewaySettings
    {
        public bool UseSandbox { get; set; }

        public string SandboxGatewayUrl { get; set; }

        public string SandboxGatewayFailoverUrl { get; set; }

        public string GatewayUrl { get; set; }

        public string GatewayFailoverUrl { get; set; }

        public string MerchantId { get; set; }

        public string RecurringMerchantId { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string TerminalId { get; set; }

        public string Bin { get; set; }
    }
}
