using cgrmodellibrary.DTOs.Category;

namespace cgrbussinesslogic.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync(
        bool? isActive);

    Task<CategoryDto> GetByIdAsync(
        int categoryId);

    Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto);

    Task<CategoryDto> UpdateAsync(
        int categoryId,
        UpdateCategoryDto dto);
}