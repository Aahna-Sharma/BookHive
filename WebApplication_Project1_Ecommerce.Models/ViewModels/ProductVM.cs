using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplication_Project1_Ecommerce.Models.ViewModels
{
    public class ProductVM
    {
        public IEnumerable<SelectListItem>
            CategoryList
        { get; set; }
        public IEnumerable<SelectListItem>
            coverTypeList
        { get; set; }
        public Product product { get; set; }

    }
}
