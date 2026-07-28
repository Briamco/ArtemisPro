using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();
}
