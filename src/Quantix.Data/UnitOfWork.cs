using Quantix.Core.Entities;
using Quantix.Core.Interfaces;
using Quantix.Data.Repositories;

namespace Quantix.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IGenericRepository<Transaction>? _transactions;
    private IGenericRepository<Category>? _categories;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<Transaction> Transactions
        => _transactions ??= new GenericRepository<Transaction>(_context);

    public IGenericRepository<Category> Categories
        => _categories ??= new GenericRepository<Category>(_context);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
