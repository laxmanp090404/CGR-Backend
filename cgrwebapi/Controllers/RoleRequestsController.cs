using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.RoleRequest;
using cgrmodellibrary.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace cgrwebapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleRequestsController : ControllerBase
{
    private readonly IRoleRequestService _roleRequestService;
    public RoleRequestsController(IRoleRequestService roleRequestService)
    {
        _roleRequestService = roleRequestService;
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("RoleRequestCreate")]
    public async Task<ActionResult<RoleRequestDto>> Create([FromBody] CreateRoleRequestDto dto)
    {
        int employeeId = User.GetEmployeeId();
        string role = User.GetRole();

        var result = await _roleRequestService.CreateAsync(dto, employeeId, role);

        return Ok(result);
    }
    
    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<RoleRequestDto>>> MyRequests()
    {
        int employeeId = User.GetEmployeeId();

        var result = await _roleRequestService.GetMyRequestsAsync(employeeId);

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResultDto<RoleRequestDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] short? statusId = null)
    {
        var result = await _roleRequestService.GetPagedAsync(page, pageSize, statusId);

        return Ok(result);
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleRequestDto>> Approve(int id, [FromBody] ApproveRoleRequestDto dto)
    {
        int adminId = User.GetEmployeeId();
        var result = await _roleRequestService.ApproveAsync(id, dto, adminId);
        return Ok(result);
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<RoleRequestDto>> Reject(int id,[FromBody] RejectRoleRequestDto dto)
    {
        int adminId = User.GetEmployeeId();
        var result =await _roleRequestService.RejectAsync(id,dto.Remarks,adminId);
        return Ok(result);
    }


}
