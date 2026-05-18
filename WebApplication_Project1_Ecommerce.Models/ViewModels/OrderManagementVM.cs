using System.Collections.Generic;

namespace WebApplication_Project1_Ecommerce.Models.ViewModels
{
    public class OrderManagementVM
    {
        public IEnumerable<OrderHeader> OrderHeaders { get; set; }
        public string SelectedMonth { get; set; }
    }
}
