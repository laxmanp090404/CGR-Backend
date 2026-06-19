using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Employee;
using cgrmodellibrary.DTOs.RoleRequest;
using cgrmodellibrary.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace cgrwebapi.Controllers;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(
        IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<EmployeeDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? roleId = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] string? search = null)
    {
        var result =
            await _employeeService.GetPagedAsync(
                page,
                pageSize,
                isActive,
                roleId,
                departmentId,
                search);

        return Ok(result);
    }

    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(
        int employeeId)
    {
        var result =
            await _employeeService.GetByIdAsync(
                employeeId);

        return Ok(result);
    }
    [EnableRateLimiting("EmployeeUpdate")]
    [HttpPut("{employeeId:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(
        int employeeId,
        UpdateEmployeeDto dto)
    {
        var result =
            await _employeeService.UpdateAsync(
                employeeId,
                dto);

        return Ok(result);
    }

    [HttpDelete("{employeeId:int}")]
    public async Task<IActionResult> Deactivate(
        int employeeId)
    {
        await _employeeService.DeactivateAsync(
            employeeId);

        return NoContent();
    }

    [HttpPatch("{employeeId:int}/restore")]
    public async Task<IActionResult> Restore(
        int employeeId)
    {
        await _employeeService.RestoreAsync(
            employeeId);

        return NoContent();
    }
}