using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.CustomTables.Types;
using CMS.DataEngine;
using CMS.Ecommerce;

namespace CMS.CustomTables.Types
{
    public class PaymentechProfileProvider
    {


        public List<PaymentechProfileItem> GetCustomerPaymentProfiles(CustomerInfo customerInfo)
        {
            var result = new List<PaymentechProfileItem>();
            var customTable = DataClassInfoProvider.GetDataClassInfo(PaymentechProfileItem.CLASS_NAME);
            if (customTable != null)
            {

                var customTableItems =
                    CustomTableItemProvider.GetItems<PaymentechProfileItem>()
                        .Where(x => x.CustomerID == customerInfo.CustomerID);
                result= customTableItems.ToList();


            }
            return result;
        }

        public void InsertPaymentechProfileItem(PaymentechProfileItem item)
        {
            CustomTableItemProvider.SetItem(item);
        }

        public void UpdatePaymentechProfileItem(PaymentechProfileItem item)
        {
            CustomTableItemProvider.SetItem(item);
        }
    }
}
