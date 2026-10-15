using Microsoft.EntityFrameworkCore;
using Quantix.Core.DTOs;
using Quantix.Core.Entities;
using Quantix.Core.Interfaces;

namespace Quantix.Service;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync(string userId, bool isAdmin = false)
    {
        var categories = await _unitOfWork.Categories
            .Where(category => category.AppUserId == userId || category.AppUserId == null)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                IconCode = c.IconCode,
                CanManage = c.AppUserId == userId || (isAdmin && c.AppUserId == null),
                IsShared = c.AppUserId == null
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<CategoryDto>>.SuccessResult(categories);
    }

    public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto, string userId)
    {
        var entity = new Category
        {
            Name = dto.Name,
            Type = dto.Type,
            IconCode = dto.IconCode,
            AppUserId = userId
        };

        await _unitOfWork.Categories.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            IconCode = entity.IconCode,
            CanManage = true,
            IsShared = false
        };

        return ApiResponse<CategoryDto>.SuccessResult(result, "Kategori oluşturuldu.");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id, string userId, bool isAdmin = false)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id);
        if (entity == null || (entity.AppUserId != userId && !(isAdmin && entity.AppUserId == null)))
            return ApiResponse<string>.FailResult("Kategori bulunamadı.");

        if (await _unitOfWork.Transactions.Where(transaction => transaction.CategoryId == id)
            .IgnoreQueryFilters().AnyAsync())
            return ApiResponse<string>.FailResult("İşlemlerde kullanılan kategori silinemez.");

        _unitOfWork.Categories.Remove(entity);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.SuccessResult("Kategori silindi.");
    }
}
