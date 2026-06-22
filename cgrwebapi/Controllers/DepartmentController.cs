using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Department;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(
        IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll(
        [FromQuery] bool? isActive)
    {
        var result =
            await _departmentService.GetAllAsync(isActive);

        return Ok(result);
    }

    [HttpGet("{departmentId:int}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int departmentId)
    {
        var result =await _departmentService.GetByIdAsync(departmentId);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(
        [FromBody] CreateDepartmentDto dto)
    {
        var result =
            await _departmentService.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { departmentId = result.DepartmentId },
            result);
    }

    [HttpPut("{departmentId:int}")]
    public async Task<ActionResult<DepartmentDto>> Update(
        int departmentId,
        [FromBody] UpdateDepartmentDto dto)
    {
        var result =
            await _departmentService.UpdateAsync(
                departmentId,
                dto);

        return Ok(result);
    }
}