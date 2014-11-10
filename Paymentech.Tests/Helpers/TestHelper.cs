using CMS.Ecommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Globalization;
using CMS.Membership;
using CMS.SiteProvider;

namespace Paymentech.Tests.Helpers
{
    public class TestHelper
    {
        private static Random random = new Random((int)DateTime.Now.Ticks);
        public static  string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public static ShoppingCartInfo CreateShoppingCartInfo(OrderInfo orderInfo)
        {
            var original =  ShoppingCartInfoProvider.GetShoppingCartInfoFromOrder(orderInfo.OrderID);
            var cartInfo = new ShoppingCartInfo();
            original.CartItems.ForEach(x=>{

                ShoppingCartInfoProvider.AddShoppingCartItem(cartInfo, x);
            
            });
            return cartInfo;

        }
        public static OrderInfo CreateOrder(int customerId, int addressId,double total, double tax,double shipping)
        {
            // Get the customer
            CustomerInfo customer = CustomerInfoProvider.GetCustomerInfo(customerId);

            // Create order addresses from customer address
            OrderAddressInfo orderBillingAddress = null;
            OrderAddressInfo orderShippingAddress = null;

            // Get the customer address
            AddressInfo customerAddress = AddressInfoProvider.GetAddressInfo(addressId);

            if (customerAddress != null)
            {
                // Get the data from customer address
                orderBillingAddress = OrderAddressInfoProvider.CreateOrderAddressInfo(customerAddress);
                orderShippingAddress = OrderAddressInfoProvider.CreateOrderAddressInfo(customerAddress);

                // Set the order addresses
                OrderAddressInfoProvider.SetAddressInfo(orderBillingAddress);
                OrderAddressInfoProvider.SetAddressInfo(orderShippingAddress);
            }

            // Get the order status
            OrderStatusInfo orderStatus = OrderStatusInfoProvider.GetOrderStatusInfo("New", "ECommerceSite");

            // Get the currency
            CurrencyInfo currency = CurrencyInfoProvider.GetCurrencyInfo("USD", "ECommerceSite");

                // Create new order object
                OrderInfo newOrder = new OrderInfo
                {
                    OrderInvoiceNumber = random.Next(1000,9999).ToString(),
                    OrderBillingAddress = orderBillingAddress,
                    OrderShippingAddress = orderShippingAddress,
                    OrderTotalPrice = total,
                    OrderTotalTax = tax,
                    OrderTotalShipping=shipping,
                    OrderDate = DateTime.Now,
                    OrderStatusID = orderStatus.StatusID,
                    OrderCustomerID = customer.CustomerID,
                    OrderSiteID = 2,
                    OrderCurrencyID = currency.CurrencyID
                };

                // Create the order
                OrderInfoProvider.SetOrderInfo(newOrder);

            return newOrder;
        }
        public static CustomerInfo CreateRegisteredCustomer(string username,string email)
        {
            // Create a new user
            UserInfo newUser = new UserInfo();

            // Set the user properties
            newUser.UserName = username;
            newUser.UserEnabled = true;
            newUser.SetPrivilegeLevel(UserPrivilegeLevelEnum.Editor);

            // Save the user
            UserInfoProvider.SetUserInfo(newUser);

            // Add user to current site
            UserInfoProvider.AddUserToSite(newUser.UserName, SiteContext.CurrentSiteName);

            // Create new customer object
            CustomerInfo newCustomer = new CustomerInfo
            {
                CustomerFirstName = "",
                CustomerLastName = "My new registered customer",
                CustomerEmail = email,
                CustomerEnabled = true,
                CustomerSiteID = SiteContext.CurrentSiteID,
                CustomerUserID = newUser.UserID
            };

            // Create the registered customer
            CustomerInfoProvider.SetCustomerInfo(newCustomer);

            return newCustomer;
        }
        public static AddressInfo CreateBillingAddress(int customerId)
        {
            // Get the customer
            CustomerInfo customer = CustomerInfoProvider.GetCustomerInfo(customerId);

            // Get the country
            CountryInfo country = CountryInfoProvider.GetCountryInfo("USA");

            // Get the state
            StateInfo state = StateInfoProvider.GetStateInfo("NewHampshire");

       
                // Create new address object
                AddressInfo newAddress = new AddressInfo
                {
                    AddressName = "My new address",
                    AddressLine1 = "1 Northeastern Blvd",
                    AddressLine2 = "Suite 100",
                    AddressCity = "Bedford",
                    AddressZip = "03109-1234",
                    AddressIsBilling = true,
                    AddressIsShipping = true,
                    AddressIsCompany = false,
                    AddressEnabled = true,
                    AddressPersonalName = customer.CustomerInfoName,
                    AddressCustomerID = customer.CustomerID,
                    AddressCountryID = country.CountryID,
                    AddressStateID = state.StateID
                };

                // Create the address
                AddressInfoProvider.SetAddressInfo(newAddress);

                return newAddress;
           
        }

        public static int GetWellKnownCustomerId()
        {
            return 78;
        }

        public static int GetWellKnownOrderId()
        {
            return 257;
        }

        
    }
}
