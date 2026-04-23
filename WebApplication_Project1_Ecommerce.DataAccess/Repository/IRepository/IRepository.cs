using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace WebApplication_Project1_Ecommerce.Utility
{
    public interface IRepository<T> where T : class
    {
        void Add (T  entity);
        void Update (T  entity);

        T Get(int id);

        IEnumerable<T> GetAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> OrderBy= null, string includeProperties = null);

        T FirstOrDefault(
          Expression<Func<T, bool>> filter = null,
          string includeProperties = null
      );

        void Remove(int id);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T>entities);
    }
}
