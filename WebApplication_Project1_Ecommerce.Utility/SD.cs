using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplication_Project1_Ecommerce.Utility
{
    public static class SD
    {
        //Roles
        public const string Role_Admin = "Admin User";
        public const string Role_Employee = "Employee User";
        public const string Role_Company = "Company User";
        public const string Role_Individual = "Individual User";

        //SESSION
        public const string Ss_CartSessionCount = "Cart Count Session";

        //CART
        public static double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity < 50)
            {
                return price;
            }
            else if (quantity < 100)
                return price50;
            return price100;
        }

        //ORDER STATUS
        public const string OrderStatusPending = "Pending";
        public const string OrderStatusApproved = "Approved";
        public const string OrderStatusInProgress = "InProgress";
        public const string OrderStatusShipped = "Shipped";
        public const string OrderStatusCancelled = "Cancelled";
        public const string OrderStatusRefunded = "Refunded";

        //PAYMENT STATUS
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayPending = "PaymentStatusDelay";
        public const string PaymentStatusRejected = "Rejected";
       
    }
}
