using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace WebApplication_Project1_Ecommerce.Models
{
    public class OrderDetail
    {
        public int id {  get; set; }
        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        public  OrderHeader OrderHeader { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]

        public Product Product { get; set; }
        public int Count { get; set; }
        public double Price { get; set; }

    }
}
