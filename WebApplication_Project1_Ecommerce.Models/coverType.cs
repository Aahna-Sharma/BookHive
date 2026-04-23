using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WebApplication_Project1_Ecommerce.Models
{
    public class coverType
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
