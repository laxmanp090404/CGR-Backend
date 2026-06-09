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

        bool canSeeInternal = _currentUserService.Role is "GRO" or "DEPARTMENT_HEAD" or "ADMIN" && complaint.RaisedByEmployeeId != _currentUserService.EmployeeId;;

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
            if (_currentUserService.Role == "EMPLOYEE")
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
        if (_currentUserService.Role == "ADMIN")
            return;

        if (_currentUserService.Role == "EMPLOYEE" &&
            complaint.RaisedByEmployeeId != _currentUserService.EmployeeId)
            throw new ForbiddenException("You are not authorized to comment or view this.");

        if (_currentUserService.Role == "GRO" &&
            complaint.RaisedByEmployeeId != _currentUserService.EmployeeId &&
            complaint.CurrentHandlerEmployeeId != _currentUserService.EmployeeId)
            throw new ForbiddenException("You are not authorized to comment or view this.");

        if (_currentUserService.Role == "DEPARTMENT_HEAD" &&
            complaint.RaisedByEmployeeId != _currentUserService.EmployeeId &&
            complaint.Category.DepartmentId != _currentUserService.DepartmentId)
            throw new ForbiddenException("You are not authorized to comment or view this.");
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