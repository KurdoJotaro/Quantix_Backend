using Quantix.Core.DTOs;

namespace Quantix.Core.Interfaces;

public interface ITransactionService
{
    Task<ApiResponse<IEnumerable<TransactionDto>>> GetUserTransactionsAsync(string userId);
    Task<ApiResponse<TransactionDto>> GetByIdAsync(int id, string userId);
    Task<ApiResponse<TransactionDto>> CreateAsync(CreateTransactionDto dto, string userId);
    Task<ApiResponse<TransactionDto>> UpdateAsync(int id, UpdateTransactionDto dto, string userId);
    Task<ApiResponse<string>> DeleteAsync(int id, string userId);
}
