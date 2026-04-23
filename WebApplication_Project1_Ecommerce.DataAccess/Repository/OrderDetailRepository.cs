using System;
using System.Collections.Generic;
using System.Text;
using WebApplication_Project1_Ecommerce.DataAccess.Data;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;

namespace WebApplication_Project1_Ecommerce.DataAccess.Repository
{
    public class OrderDetailRepository: Repository<OrderDetail>, IOrderDetailRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderDetailRepository(ApplicationDbContext context):base(context)
        {
            _context = context;
        }
    }
    
}
