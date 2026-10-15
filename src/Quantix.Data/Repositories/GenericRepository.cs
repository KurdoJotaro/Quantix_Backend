using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Quantix.Core.Entities;
using Quantix.Core.Interfaces;

namespace Quantix.Data.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FirstOrDefaultAsync(entity => entity.Id == id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public IQueryable<T> Where(Expression<Func<T, bool>> predicate)
        => _dbSet.Where(predicate);

    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Remove(T entity)
    {
        entity.IsDeleted = true;
        _dbSet.Update(entity);
    }
}
