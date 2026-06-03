using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Category;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(int? departmentId, bool? isActive)
    {
        var categories = await _categoryRepository.GetByDepartmentAsync(departmentId, isActive);
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var cat = await _categoryRepository.Get(id);
        return MapToDto(cat);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var cat = new Category
        {
            CategoryName = dto.CategoryName,
            DepartmentId = dto.DepartmentId,
            DefaultPriorityId = dto.DefaultPriorityId,
            SlaHours = dto.SlaHours,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _categoryRepository.Create(cat);
        return MapToDto(created);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var cat = await _categoryRepository.Get(id);
        cat.CategoryName = dto.CategoryName;
        cat.DefaultPriorityId = dto.DefaultPriorityId;
        cat.SlaHours = dto.SlaHours;
        cat.IsActive = dto.IsActive;
        var updated = await _categoryRepository.Update(cat, id);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _categoryRepository.Delete(id);
    }

    private static CategoryDto MapToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        CategoryName = c.CategoryName,
        DepartmentId = c.DepartmentId,
        DepartmentName = c.Department?.DepartmentName ?? string.Empty,
        DefaultPriorityId = c.DefaultPriorityId,
        DefaultPriorityName = c.DefaultPriority?.PriorityName ?? string.Empty,
        SlaHours = c.SlaHours,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };
}
