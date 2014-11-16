using CMS.CustomTables;
using CMS.CustomTables.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Tests
{
    public class TestInitializer
    {
        private static bool _isInitialized;
        public static void Initialize(){
            if (!_isInitialized)
            {
                CustomTableItemGenerator.RegisterCustomTable<PaymentechProfileItem>(PaymentechProfileItem.CLASS_NAME);
                CustomTableItemGenerator.RegisterCustomTable<PaymentechTransactionItem>(PaymentechTransactionItem.CLASS_NAME);
                _isInitialized = true;
            }
           
        }
      
    }
}
