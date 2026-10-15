using Microsoft.EntityFrameworkCore;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;
using Quantix.Core.Interfaces;

namespace Quantix.Service;

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<TransactionDto>>> GetUserTransactionsAsync(string userId)
    {
        var transactions = await _unitOfWork.Transactions
            .Where(t => t.AppUserId == userId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Description = t.Description,
                Date = t.Date,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                CategoryType = t.Category.Type,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<TransactionDto>>.SuccessResult(transactions);
    }

    public async Task<ApiResponse<TransactionDto>> GetByIdAsync(int id, string userId)
    {
        var transaction = await _unitOfWork.Transactions
            .Where(t => t.Id == id && t.AppUserId == userId)
            .Include(t => t.Category)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Description = t.Description,
                Date = t.Date,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                CategoryType = t.Category.Type,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
            return ApiResponse<TransactionDto>.FailResult("İşlem bulunamadı.");

        return ApiResponse<TransactionDto>.SuccessResult(transaction);
    }

    public async Task<ApiResponse<TransactionDto>> CreateAsync(CreateTransactionDto dto, string userId)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (category == null)
            return ApiResponse<TransactionDto>.FailResult("Kategori bulunamadı.");

        var entity = new Transaction
        {
            Amount = dto.Amount,
            Description = dto.Description,
            Date = dto.Date,
            CategoryId = dto.CategoryId,
            AppUserId = userId
        };

        await _unitOfWork.Transactions.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var result = new TransactionDto
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Description = entity.Description,
            Date = entity.Date,
            CategoryId = entity.CategoryId,
            CategoryName = category.Name,
            CategoryType = category.Type,
            CreatedAt = entity.CreatedAt
        };

        return ApiResponse<TransactionDto>.SuccessResult(result, "İşlem oluşturuldu.");
    }

    public async Task<ApiResponse<TransactionDto>> UpdateAsync(int id, UpdateTransactionDto dto, string userId)
    {
        var entity = await _unitOfWork.Transactions
            .Where(t => t.Id == id && t.AppUserId == userId)
            .FirstOrDefaultAsync();

        if (entity == null)
            return ApiResponse<TransactionDto>.FailResult("İşlem bulunamadı.");

        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
        if (category == null)
            return ApiResponse<TransactionDto>.FailResult("Kategori bulunamadı.");

        entity.Amount = dto.Amount;
        entity.Description = dto.Description;
        entity.Date = dto.Date;
        entity.CategoryId = dto.CategoryId;

        _unitOfWork.Transactions.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        var result = new TransactionDto
        {
            Id = entity.Id,
            Amount = entity.Amount,
            Description = entity.Description,
            Date = entity.Date,
            CategoryId = entity.CategoryId,
            CategoryName = category.Name,
            CategoryType = category.Type,
            CreatedAt = entity.CreatedAt
        };

        return ApiResponse<TransactionDto>.SuccessResult(result, "İşlem güncellendi.");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id, string userId)
    {
        var entity = await _unitOfWork.Transactions
            .Where(t => t.Id == id && t.AppUserId == userId)
            .FirstOrDefaultAsync();

        if (entity == null)
            return ApiResponse<string>.FailResult("İşlem bulunamadı.");

        _unitOfWork.Transactions.Remove(entity);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.SuccessResult("İşlem silindi.");
    }
}
