using Quantix.Core.DTOs;

namespace Quantix.Core.Interfaces;

public interface ICategoryService
{
    Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllAsync(string userId, bool isAdmin = false);
    Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto, string userId);
    Task<ApiResponse<string>> DeleteAsync(int id, string userId, bool isAdmin = false);
}
