using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Comment;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class ComplaintCommentService : IComplaintCommentService
{
    private readonly IComplaintCommentRepository _commentRepository;
    private readonly IComplaintRepository _complaintRepository;

    public ComplaintCommentService(
        IComplaintCommentRepository commentRepository,
        IComplaintRepository complaintRepository)
    {
        _commentRepository = commentRepository;
        _complaintRepository = complaintRepository;
    }

    public async Task<IEnumerable<ComplaintCommentDto>> GetByComplaintIdAsync(
        int complaintId, int currentEmployeeId, string role)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        var comments = await _commentRepository.GetByComplaintIdAsync(complaintId);

        // Non-GRO/Admin/DeptHead employees cannot see internal comments
        bool canSeeInternal = role == "GRO" || role == "DEPARTMENT_HEAD" || role == "ADMIN";

        return comments
            .Where(c => canSeeInternal || !c.IsInternal)
            .Select(MapToDto);
    }

    public async Task<ComplaintCommentDto> AddCommentAsync(
        int complaintId, CreateComplaintCommentDto dto, int employeeId, string role)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        // Only GRO/Admin/DeptHead can add internal comments
        if (dto.IsInternal && role == "EMPLOYEE")
            throw new ForbiddenException("Employees cannot add internal comments.");

        // Employees can only comment on their own complaints
        if (role == "EMPLOYEE" && complaint.RaisedByEmployeeId != employeeId)
            throw new ForbiddenException("You can only comment on your own complaints.");

        var comment = new ComplaintComment
        {
            ComplaintId = complaintId,
            CommentText = dto.CommentText,
            CommentedBy = employeeId,
            IsInternal = dto.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.Create(comment);

        // Reload with navigation
        var comments = await _commentRepository.GetByComplaintIdAsync(complaintId);
        var reloaded = comments.FirstOrDefault(c => c.CommentId == created.CommentId);
        return MapToDto(reloaded ?? created);
    }

    private static ComplaintCommentDto MapToDto(ComplaintComment c) => new()
    {
        CommentId = c.CommentId,
        ComplaintId = c.ComplaintId,
        CommentText = c.CommentText,
        CommentedBy = c.CommentedBy,
        CommentedByName = c.CommentedByNavigation?.EmployeeName ?? string.Empty,
        IsInternal = c.IsInternal,
        CreatedAt = c.CreatedAt
    };
}
