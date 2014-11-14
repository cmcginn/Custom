using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.CustomTables;
using CMS.CustomTables.Types;
using CMS.DataEngine;
using CMS.Ecommerce;

namespace Paymentech.Data
{
    public class PaymentechTransactionProvider
    {
        public List<PaymentechTransactionItem> GetCustomerTransactions(CustomerInfo customerInfo)
        {
            List<PaymentechTransactionItem> result = null;
            var customTable = DataClassInfoProvider.GetDataClassInfo(PaymentechTransactionItem.CLASS_NAME);
            if (customTable != null)
            {

                var customTableItems =
                    CustomTableItemProvider.GetItems<PaymentechTransactionItem>()
                        .Where(x => x.CustomerID == customerInfo.CustomerID);
                result = customTableItems.ToList();


            }
            return result;
        }
        public List<PaymentechTransactionItem> GetOrderTransactionsForOrder(OrderInfo order)
        {
            List<PaymentechTransactionItem> result = null;
            var customTable = DataClassInfoProvider.GetDataClassInfo(PaymentechTransactionItem.CLASS_NAME);
            if (customTable != null)
            {

                var customTableItems =
                    CustomTableItemProvider.GetItems<PaymentechTransactionItem>()
                        .Where(x => x.OrderID == order.OrderID);
                result = customTableItems.ToList();


            }
            return result;
        }
        public void InsertCustomerTransaction(PaymentechTransactionItem item)
        {
            CustomTableItemProvider.SetItem(item);
        }
    }
}
