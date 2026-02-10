using Quantix.Core.Entities;

namespace Quantix.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Transaction> Transactions { get; }
    IGenericRepository<Category> Categories { get; }
    Task<int> SaveChangesAsync();
}
