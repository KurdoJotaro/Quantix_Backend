using Quantix.Core.DTOs;

namespace Quantix.Core.Interfaces;

public interface ICategoryService
{
    Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync();
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto);
    Task<ApiResponse<string>> DeleteAsync(int id);
}
