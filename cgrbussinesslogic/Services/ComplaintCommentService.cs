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
    private readonly ICurrentUserService _currentUserService;

    #region STATUS IDS
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;
    #endregion

    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;
    public ComplaintCommentService(
        IComplaintCommentRepository commentRepository,
        IComplaintRepository complaintRepository,
        ICurrentUserService userService)
    {
        _commentRepository = commentRepository;
        _complaintRepository = complaintRepository;
        _currentUserService = userService;
    }

    public async Task<IEnumerable<ComplaintCommentDto>> GetByComplaintIdAsync(int complaintId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        ValidateCommentAccess(complaint);

        var comments = await _commentRepository.GetByComplaintIdAsync(complaintId);

        bool canSeeInternal = _currentUserService.RoleId is ROLE_GRO or ROLE_DEPARTMENT_HEAD or ROLE_ADMIN && complaint.RaisedByEmployeeId != _currentUserService.EmployeeId;;

        return comments
            .Where(c => canSeeInternal || !c.IsInternal)
            .Select(MapToDto);
    }

    public async Task<ComplaintCommentDto> AddCommentAsync(int complaintId, CreateComplaintCommentDto dto)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        ValidateCommentAccess(complaint);

        if (complaint.StatusId is STATUS_CLOSED or STATUS_REJECTED or STATUS_EXTERNALLY_ESCALATED)
            throw new BusinessRuleException($"Cannot comment on complaints in status {complaint.Status?.StatusName}");

        if (dto.IsInternal)
        {
            if (_currentUserService.RoleId == ROLE_EMPLOYEE)
                throw new ForbiddenException("Employees cannot add internal comments.");

            if (complaint.RaisedByEmployeeId == _currentUserService.EmployeeId)
                throw new ForbiddenException("Cannot add internal comments on your own complaint.");
        }
        var comment = new ComplaintComment
        {
            ComplaintId = complaintId,
            CommentText = dto.CommentText,
            CommentedBy = _currentUserService.EmployeeId,
            IsInternal = dto.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.Create(comment);
        return MapToDto(created);
    }

    private void ValidateCommentAccess(Complaint complaint)
    {
        if (_currentUserService.RoleId == ROLE_ADMIN)
            return;

        // All other roles are permitted to view comments. Specific visibility is handled in GetByComplaintIdAsync.
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