using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using WebApplication_Project1_Ecommerce.DataAccess.Data;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;

namespace WebApplication_Project1_Ecommerce.DataAccess.Repository
{
    public class coverTypeRepository : Repository<coverType>, IcoverTypeRepository
    {
        private readonly ApplicationDbContext _context;
        public coverTypeRepository(ApplicationDbContext context) : base(context)
        {
           _context = context;
        }
    }
}
