using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Category;
using cgrmodellibrary.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? departmentId = null)
    {
        if (page.HasValue && pageSize.HasValue)
        {
            var pagedResult = await _categoryService.GetPagedAsync(page.Value, pageSize.Value, isActive, departmentId);
            return Ok(pagedResult);
        }
        else
        {
            var result = await _categoryService.GetAllAsync(isActive, departmentId);
            return Ok(result);
        }
    }

    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(
        int categoryId)
    {
        var result =
            await _categoryService.GetByIdAsync(categoryId);

        return Ok(result);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto)
    {
        var result =
            await _categoryService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { categoryId = result.CategoryId },
            result);
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{categoryId:int}")]
    public async Task<ActionResult<CategoryDto>> Update(
        int categoryId,
        UpdateCategoryDto dto)
    {
        var result =
            await _categoryService.UpdateAsync(
                categoryId,
                dto);

        return Ok(result);
    }
}