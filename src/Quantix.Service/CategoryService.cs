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

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync()
    {
        var categories = await _unitOfWork.Categories
            .Where(_ => true)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                IconCode = c.IconCode
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<CategoryDto>>.SuccessResult(categories);
    }

    public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        var entity = new Category
        {
            Name = dto.Name,
            Type = dto.Type,
            IconCode = dto.IconCode
        };

        await _unitOfWork.Categories.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            IconCode = entity.IconCode
        };

        return ApiResponse<CategoryDto>.SuccessResult(result, "Kategori oluşturuldu.");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var entity = await _unitOfWork.Categories.GetByIdAsync(id);
        if (entity == null)
            return ApiResponse<string>.FailResult("Kategori bulunamadı.");

        if (await _unitOfWork.Transactions.Where(transaction => transaction.CategoryId == id)
            .IgnoreQueryFilters().AnyAsync())
            return ApiResponse<string>.FailResult("İşlemlerde kullanılan kategori silinemez.");

        _unitOfWork.Categories.Remove(entity);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.SuccessResult("Kategori silindi.");
    }
}
