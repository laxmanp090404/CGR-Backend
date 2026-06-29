using cgrmodellibrary.DTOs.Category;
using cgrmodellibrary.DTOs.Common;

namespace cgrbussinesslogic.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync(
        bool? isActive, int? departmentId);

    Task<PagedResultDto<CategoryDto>> GetPagedAsync(
        int page, int pageSize, bool? isActive, int? departmentId);

    Task<CategoryDto> GetByIdAsync(
        int categoryId);

    Task<CategoryDto> CreateAsync(
        CreateCategoryDto dto);

    Task<CategoryDto> UpdateAsync(
        int categoryId,
        UpdateCategoryDto dto);
}