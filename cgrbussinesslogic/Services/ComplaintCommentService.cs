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

        if(_currentUserService.RoleId == ROLE_DEPARTMENT_HEAD)
        {
            if(complaint.Category.DepartmentId != _currentUserService.DepartmentId &&
               complaint.RaisedByEmployeeId != _currentUserService.EmployeeId)
            {
                throw new ForbiddenException("You do not have access to view comments on this complaint.");
            }
        }
        if(_currentUserService.RoleId == ROLE_GRO || _currentUserService.RoleId == ROLE_EMPLOYEE)
        {
            if(complaint.RaisedByEmployeeId != _currentUserService.EmployeeId &&
               complaint.CurrentHandlerEmployeeId != _currentUserService.EmployeeId)
            {
                throw new ForbiddenException("You do not have access to view comments on this complaint.");
            }
        }
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
        bool isRaiserorHandler = complaint.RaisedByEmployeeId == _currentUserService.EmployeeId ||
                                    complaint.CurrentHandlerEmployeeId == _currentUserService.EmployeeId;
        if(!isRaiserorHandler && _currentUserService.RoleId!=ROLE_ADMIN)
            throw new ForbiddenException("Only the complaint raiser,Handler or Admin can comment on this complaint.");
        

        if (dto.IsInternal)
        {
            if (_currentUserService.RoleId == ROLE_EMPLOYEE)
                throw new ForbiddenException("Employees cannot add internal comments.");

            if (complaint.RaisedByEmployeeId == _currentUserService.EmployeeId)
                throw new ForbiddenException("Cannot add internal comments on your own complaint.");
        }
        if(await _commentRepository.ExistsRecentDuplicateAsync(_currentUserService.EmployeeId, dto.CommentText, TimeSpan.FromMinutes(5)))
            throw new BusinessRuleException("You have already added a similar comment recently. Please wait if you want to add the same comment again.");
        var comment = new ComplaintComment
        {
            ComplaintId = complaintId,
            CommentText = dto.CommentText,
            CommentedBy = _currentUserService.EmployeeId,
            IsInternal = dto.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.Create(comment);
        var reloaded = await _commentRepository.Get(created.CommentId)
            ?? created;
        return MapToDto(reloaded);
    }

    private void ValidateCommentAccess(Complaint complaint)
    {
        if (_currentUserService.RoleId == ROLE_ADMIN)
            return;

        
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