using WebApplication_Project1_Ecommerce.DataAccess.Data;
using WebApplication_Project1_Ecommerce.DataAccess.Repository.IRepository;
using WebApplication_Project1_Ecommerce.Models;

namespace WebApplication_Project1_Ecommerce.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}