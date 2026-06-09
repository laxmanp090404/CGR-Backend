using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ComplaintController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public ComplaintController(
        IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }
    //ratelimiting required
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ComplaintDto>> Create(
        [FromForm] CreateComplaintDto dto)
    {
        var result =
            await _complaintService.CreateAsync(
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { complaintId = result.ComplaintId },
            result);
    }

    [HttpGet("{complaintId:int}")]
    public async Task<ActionResult<ComplaintDto>> GetById(
        int complaintId)
    {
        var result =
            await _complaintService.GetByIdAsync(
                complaintId);

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ComplaintDashboardDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? statusId = null,
        [FromQuery] int? priorityId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] string? search = null)
    {
        var result =
            await _complaintService.GetPagedAsync(
                page,
                pageSize,
                statusId,
                priorityId,
                categoryId,
                departmentId,
                search);

        return Ok(result);
    }

    [HttpGet("{complaintId:int}/history")]
    public async Task<ActionResult<IEnumerable<ComplaintHistoryDto>>> GetHistory(
        int complaintId)
    {
        var result =
            await _complaintService.GetHistoryAsync(
                complaintId);

        return Ok(result);
    }

    [Authorize(Roles = "GRO,DEPARTMENT_HEAD,ADMIN")]
    [HttpPut("{complaintId:int}/start-progress")]
    public async Task<ActionResult> StartProgress(
        int complaintId)
    {
        await _complaintService.StartProgressAsync(
            complaintId);

        return NoContent();
    }

    [Authorize(Roles = "GRO,DEPARTMENT_HEAD,ADMIN")]
    [HttpPut("{complaintId:int}/resolve")]
    public async Task<ActionResult> Resolve(
        int complaintId,
        ResolveComplaintDto dto)
    {
        await _complaintService.ResolveAsync(
            complaintId,
            dto);

        return NoContent();
    }

    [HttpPut("{complaintId:int}/close")]
    public async Task<ActionResult> Close(
        int complaintId,
        CloseComplaintDto dto)
    {
        await _complaintService.CloseAsync(
            complaintId,
            dto);

        return NoContent();
    }

    [HttpPut("{complaintId:int}/reopen")]
    public async Task<ActionResult> Reopen(
        int complaintId,
        ReopenComplaintDto dto)
    {
        await _complaintService.ReopenAsync(
            complaintId,
            dto);

        return NoContent();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("{complaintId:int}/assign")]
    public async Task<ActionResult> Assign(
        int complaintId,
        AssignComplaintDto dto)
    {
        await _complaintService.AssignAsync(
            complaintId,
            dto);

        return NoContent();
    }

    [Authorize(Roles = "GRO,DEPARTMENT_HEAD")]
    [HttpPut("{complaintId:int}/escalate")]
    public async Task<ActionResult> Escalate(
        int complaintId,
        EscalateComplaintDto dto)
    {
        await _complaintService.EscalateAsync(
            complaintId,
            dto);

        return NoContent();
    }
}