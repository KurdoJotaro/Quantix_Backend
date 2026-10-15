using System.ComponentModel.DataAnnotations;
using Quantix.Core.Enums;

namespace Quantix.Core.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? IconCode { get; set; }
    public bool CanManage { get; set; }
    public bool IsShared { get; set; }
}

public class CreateCategoryDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [EnumDataType(typeof(CategoryType))]
    public CategoryType Type { get; set; }

    public string? IconCode { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
}
