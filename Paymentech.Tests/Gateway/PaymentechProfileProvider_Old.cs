//using CMS.CustomTables;
//using CMS.DataEngine;
//using CMS.Ecommerce;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Web;

//namespace CMSCustom.revolutiongolf.ecommerce.paymentech.Providers
//{
//    public class PaymentechProfile
//    {
//        public int ItemId { get; set; }

//        public bool IsActive { get; set; }

//        public int CustomerID { get; set; }

//        public long CustomerRefNum { get; set; }

//        public string CardholderName { get; set; }

//        public long MerchantID { get; set; }

//        public string MaskedAccountNumber { get; set; }

//        public bool IsRecurring { get; set; }

//        public string Expiration { get; set; }

//        public string CardBrand { get; set; }

//        public DateTime LastTransactionUtc { get; set; }
//    }
//    public class PaymentechProfileProvider
//    {
//        #region public variables
        
//        string customTableClassName = "RG.PaymentechProfile";

//        #endregion

//        #region utility
//        string GetMaskedCardNumber(string source)
//        {
//            return source.Substring(source.Length - 4);
//        }
//        #endregion
//        public List<PaymentechProfile> GetCustomerPaymentProfiles(CustomerInfo customerInfo)
//        {
//            var result = new List<PaymentechProfile>();
//            var customTable = DataClassInfoProvider.GetDataClassInfo(customTableClassName);
//            if(customTable != null)
//            {
//                string where = string.Format("CustomerID={0}", customerInfo.CustomerID);
//                DataSet customTableItems = CustomTableItemProvider.GetItems(customTableClassName, where);
//                foreach (DataRow customTableItemDr in customTableItems.Tables[0].Rows)
//                {
//                    var profile = new PaymentechProfile();
//                    profile.ItemId = customTableItemDr.Field<int>("ItemId");              
//                    profile.CustomerID = customTableItemDr.Field<int>("CustomerID");
//                    profile.CustomerRefNum = customTableItemDr.Field<long>("CustomerRefNum");
//                    profile.CardBrand = customTableItemDr.Field<string>("CardBrand");
//                    profile.CardholderName = customTableItemDr.Field<string>("CardholderName");
//                    profile.Expiration = customTableItemDr.Field<string>("Expiration");
//                    profile.IsRecurring = customTableItemDr.Field<bool>("IsRecurring");
//                    profile.IsActive = customTableItemDr.Field<bool>("IsActive");
//                    profile.MaskedAccountNumber = customTableItemDr.Field<string>("MaskedAccountNumber");
//                    profile.MerchantID = customTableItemDr.Field<long>("MerchantID");
//                    profile.LastTransactionUtc = customTableItemDr.Field<DateTime>("LastTransactionUtc");
//                    result.Add(profile);

//                }
//            }
//            return result;
//        }
//        public void InsertPaymentProfile(CustomerInfo customerInfo, long customerRefNum, long merchantID, string cardBrand, string cardholderName, string accountNumber, string expiration,  DateTime lastTransactionUtc, bool isRecurring, bool isActive)
//        {
//            var accountNum = GetMaskedCardNumber(accountNumber);
//            var existing = GetCustomerPaymentProfiles(customerInfo).Where(x => x.MaskedAccountNumber == accountNum && x.Expiration == expiration).Any();
//            if (existing)
//                throw new System.Exception("A Duplicate Payment Profile Exists for Card Number and Expiration");

//            var customTable = DataClassInfoProvider.GetDataClassInfo(customTableClassName);
//            if (customTable != null)
//            {
                
//                var newCustomTableItem = CustomTableItem.New(customTableClassName);
//                newCustomTableItem.SetValue("CustomerID", customerInfo.CustomerID);
//                newCustomTableItem.SetValue("CustomerRefNum", customerRefNum);
//                if(!String.IsNullOrEmpty(cardholderName))
//                    newCustomTableItem.SetValue("CardholderName", cardholderName);
//                newCustomTableItem.SetValue("MerchantID", merchantID);
//                newCustomTableItem.SetValue("MaskedAccountNumber", accountNum);
//                newCustomTableItem.SetValue("IsRecurring", isRecurring);
//                newCustomTableItem.SetValue("Expiration", expiration);
//                if(!String.IsNullOrEmpty(cardBrand))
//                    newCustomTableItem.SetValue("CardBrand", cardBrand);
//                newCustomTableItem.SetValue("LastTransactionUtc", lastTransactionUtc);
//                newCustomTableItem.SetValue("IsActive", isActive);
//                newCustomTableItem.ItemOrder = CustomTableItemProvider.GetLastItemOrder(customTableClassName) + 1;
//                newCustomTableItem.Insert();
//            }
//        }
//    }
//}