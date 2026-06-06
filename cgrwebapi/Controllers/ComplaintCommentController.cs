using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Comment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ComplaintCommentController : ControllerBase
{
    private readonly IComplaintCommentService _commentService;

    public ComplaintCommentController(
        IComplaintCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("complaint/{complaintId:int}")]
    public async Task<ActionResult<IEnumerable<ComplaintCommentDto>>> GetByComplaintId(int complaintId)
    {
        var result =await _commentService.GetByComplaintIdAsync(complaintId);

        return Ok(result);
    }

    [HttpPost("complaint/{complaintId:int}")]
    public async Task<ActionResult<ComplaintCommentDto>> AddComment(int complaintId,CreateComplaintCommentDto dto)
    {
        var result =await _commentService.AddCommentAsync(complaintId,dto);

        return CreatedAtAction(nameof(GetByComplaintId),new { complaintId },result);
    }
}