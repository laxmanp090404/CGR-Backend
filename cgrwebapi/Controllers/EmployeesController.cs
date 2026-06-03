using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Employee;
using cgrmodellibrary.DTOs.RoleRequest;
using cgrmodellibrary.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;
[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IRoleRequestService _roleRequestService;
    public EmployeesController(IEmployeeService employeeService,IRoleRequestService roleRequestService)
    {
        _employeeService = employeeService;
        _roleRequestService =roleRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<EmployeeDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? roleId = null, [FromQuery] int? departmentId = null, [FromQuery] string? search = null)
    {
        var result = await _employeeService.GetPagedAsync(page, pageSize, roleId, departmentId, search);
        return Ok(new PagedResultDto<EmployeeDto>
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id)
    {
        var result = await _employeeService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var employeeId = User.GetEmployeeId();
        var role = User.GetRole();
        var result = await _employeeService.UpdateAsync(id, dto, employeeId, role);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> Deactivate(int id)
    {
        var adminId = User.GetEmployeeId();
        var role = User.GetRole();
        await _employeeService.DeactivateAsync(id, adminId, role);
        return NoContent();
    }

    [HttpPatch("{id:int}/reactivate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<object>> Reactivate(int id)
    {
        var adminId = User.GetEmployeeId();
        await _employeeService.ReactivateAsync(id, adminId);
        return NoContent();
    }


    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id}/role")]
    public async Task<ActionResult<RoleRequestDto>> ChangeRole(int id, [FromBody] ManualRoleChangeDto dto)
    {
        int adminId = User.GetEmployeeId();

        var result = await _roleRequestService.ManualRoleChangeAsync(id, dto, adminId);

        return Ok(result);
    }
}
