using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.ComplaintRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[Route("api/complaint-requests")]
[ApiController]
[Authorize]
public class ComplaintRequestController : ControllerBase
{
    private readonly IComplaintRequestService _complaintRequestService;
    private readonly ICurrentUserService _currentUserService;
    private const short ROLE_ADMIN = 4;
    public ComplaintRequestController(
        IComplaintRequestService complaintRequestService,
        ICurrentUserService currentUserService)
    {
        _complaintRequestService = complaintRequestService;
        _currentUserService = currentUserService;
    }
    // create a complaint rejection request
    [Authorize(Roles = "GRO,DEPARTMENT_HEAD")]
[HttpPost("complaints/{complaintId:int}")]
public async Task<ActionResult<ComplaintRequestDto>> Create(int complaintId,CreateComplaintRequestDto dto)
{
    var result = await _complaintRequestService.CreateAsync(
            complaintId,
            dto);

    return CreatedAtAction(
        nameof(GetPaged),
        new { requestId = result.RequestId },
        result);
}
// review the request - approve or reject only by admin
[Authorize(Roles = "ADMIN")]
[HttpPut("{requestId:int}/review")]
public async Task<ActionResult>Review(int requestId,ReviewComplaintRequestDto dto)
{
    await _complaintRequestService.ReviewAsync(
        requestId,
        dto);

    return NoContent();
}

[HttpGet]
public async Task<ActionResult<PagedResultDto<ComplaintRequestDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] short? statusId = null)
{
    int? requestedBy = null;

    if (_currentUserService.RoleId != ROLE_ADMIN)
    {
        requestedBy =_currentUserService.EmployeeId;
    }

    var result =
        await _complaintRequestService.GetPagedAsync(
            page,
            pageSize,
            statusId,
            requestedBy);

    return Ok(result);
}
}
