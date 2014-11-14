using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public class RecurringCustomerProfile:CustomerProfile
    {

        public RecurringFrequency RecurringFrequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double RecurringAmount { get; set; }

    }

    public enum RecurringFrequency
    {
        None=0,
        Monthly=1,
        Yearly=2,
        Lifetime=3
        
    }
}
