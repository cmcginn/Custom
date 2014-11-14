using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public class CardInfo
    {
        public string CardholderName { get; set; }
        public string CardNumber { get; set; }
        public string MaskedCardNumber { get; set; }
        //MMYYYY
        public string ExpirationDate { get; set; }
        public string CCV { get; set; }
        public string CardBrand { get; set; }
    }
}
