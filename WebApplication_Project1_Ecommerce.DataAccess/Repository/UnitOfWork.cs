using System;
using System.Collections.Generic;
using System.Text;
using WebApplication_Project1_Ecommerce.DataAccess.Data;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;


namespace WebApplication_Project1_Ecommerce.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public ICategoryRepository CategoryRepository { get; private set; }

        public IcoverTypeRepository CoverTypeRepository { get; private set; }

        public IProductRepository ProductRepository { get; private set; }
        public ICompanyRepository CompanyRepository { get; private set; }
        public IApplicationUserRepository ApplicationUserRepository { get; private set; }

        public IShoppingCartRepository ShoppingCartRepository {  get; private set; }
        public IOrderHeaderRepository OrderHeaderRepository {  get; private set; }
        public IOrderDetailRepository OrderDetailRepository {  get; private set; }



        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            CategoryRepository = new CategoryRepository(context);
            CoverTypeRepository = new coverTypeRepository(context);
            ProductRepository = new ProductRepository(context);
            CompanyRepository = new CompanyRepository(context);
            ApplicationUserRepository = new ApplicationUserRepository(context);
            ShoppingCartRepository = new ShoppingCartRepository(context);
            OrderHeaderRepository = new OrderHeaderRepository(context);
            OrderDetailRepository = new OrderDetailRepository(context);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
