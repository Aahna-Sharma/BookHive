using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WebApplication_Project1_Ecommerce.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required] 
        public string Title { get; set; }
        [Required] 
        public string Description { get; set; }
        [Required] 
        public string Author { get; set; }
        [Required] 
        
        public string ISBN { get; set; }
        [Required]
        [Range(1, 1000)]
        public double ListPrice { get; set; }
        [Required]
        [Range(1, 1000)]
        public double Price { get; set; }
        [Required]
        [Range(1, 1000)]
        public double Price50 { get; set; }
        [Required]
        [Range(1, 1000)]
        public double Price100 { get; set; }

        [Display (Name ="Image Url")]
        public string ImageUrl { get; set; }
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        [Required]
        [Display(Name = "CoverType")]
        public int CoverTypeId { get; set; }
        public coverType coverType { get; set; }

    }
}
