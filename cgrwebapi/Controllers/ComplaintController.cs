using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace cgrwebapi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ComplaintController : ControllerBase
{
    private readonly IComplaintService _complaintService;
    private readonly IComplaintAttachmentService _attachmentService;

    public ComplaintController(
        IComplaintService complaintService,
        IComplaintAttachmentService attachmentService)
    {
        _complaintService = complaintService;
        _attachmentService = attachmentService;
    }
    
    [HttpPost]
    [EnableRateLimiting("ComplaintCreate")]
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
        [FromQuery] string? search = null,
        [FromQuery] bool? raisedByMe = false)
    {
        var result =
            await _complaintService.GetPagedAsync(
                page,
                pageSize,
                statusId,
                priorityId,
                categoryId,
                departmentId,
                search,
                raisedByMe);

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
    [EnableRateLimiting("ComplaintRequestReopen")]
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
    [EnableRateLimiting("ComplaintEscalate")]
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

    [HttpGet("{complaintId}/assignment-history")]
public async Task<ActionResult<
    IEnumerable<ComplaintAssignmentHistoryDto>>>
    GetAssignmentHistory(int complaintId)
{
    return Ok(
        await _complaintService
            .GetAssignmentHistoryAsync(
                complaintId));
}
[HttpGet("{complaintId}/escalations")]
public async Task<ActionResult<
    IEnumerable<ComplaintEscalationDto>>>
    GetEscalationHistory(int complaintId)
{
    return Ok(
        await _complaintService
            .GetEscalationHistoryAsync(
                complaintId));
}

[HttpGet("my-work-queue")]
public async Task<ActionResult<PagedResultDto<ComplaintDashboardDto>>>
    GetMyWorkQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? statusId = null,
        [FromQuery] int? priorityId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] string? search = null)
{
    var result =
        await _complaintService.GetMyWorkQueueAsync(
            page,
            pageSize,
            statusId,
            priorityId,
            categoryId,
            departmentId,
            search);

    return Ok(result);
}

    [HttpGet("attachments")]
    public async Task<IActionResult> GetAttachment([FromQuery] string filePath)
    {
        var (physicalPath, mimeType, fileName) = await _attachmentService.GetAttachmentFileByPathAsync(filePath);

        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = fileName,
            Inline = true
        };
        Response.Headers["Content-Disposition"] = contentDisposition.ToString();

        return PhysicalFile(physicalPath, mimeType);
    }
}