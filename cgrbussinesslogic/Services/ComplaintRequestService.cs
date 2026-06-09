using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.ComplaintRequest;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class ComplaintRequestService : IComplaintRequestService
{
    private const short REQUEST_TYPE_REJECTION = 1;

    #region ROLE IDs
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;
    #endregion

    #region Request Statuses

    private const short REQUEST_STATUS_PENDING = 1;
    private const short REQUEST_STATUS_APPROVED = 2;
    private const short REQUEST_STATUS_REJECTED = 3;

    #endregion
    #region  complaint statuses
    private const short STATUS_REJECTED = 7;
    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_REOPENED = 8;
    private const short STATUS_RESOLVED = 5;
    #endregion
    private const short NOTIF_COMPLAINT_REJECTED = 6;
    private const short NOTIF_COMPLAINT_REOPENED = 7;

    private readonly IComplaintRequestRepository _requestRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IComplaintAssignmentRepository _assignmentRepository;
    private readonly IComplaintHistoryRepository _historyRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IComplaintAssignmentEngine _assignmentEngine;
    private readonly CGRContext _context;

    public ComplaintRequestService(
        IComplaintRequestRepository requestRepository,
        IComplaintRepository complaintRepository,
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IComplaintAssignmentRepository assignmentRepository,
        IComplaintHistoryRepository historyRepository,
        IEmployeeRepository employeeRepository,
        IComplaintAssignmentEngine assignmentEngine,
        CGRContext context
        )
    {
        _requestRepository = requestRepository;
        _complaintRepository = complaintRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _assignmentRepository = assignmentRepository;
        _historyRepository = historyRepository;
        _employeeRepository = employeeRepository;
        _assignmentEngine = assignmentEngine;
        _context = context;
    }
    public async Task<ComplaintRequestDto> CreateAsync(int complaintId, CreateComplaintRequestDto dto)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId}");

        if (complaint.CurrentHandlerEmployeeId != _currentUserService.EmployeeId)
        {
            throw new ForbiddenException("Only the current handler can raise a rejection request.");
        }

        if (_currentUserService.RoleId != ROLE_GRO &&
            _currentUserService.RoleId != ROLE_DEPARTMENT_HEAD)
        {
            throw new ForbiddenException("Only GROs and Department Heads can create rejection requests.");
        }

        if (dto.RequestTypeId != REQUEST_TYPE_REJECTION)
        {
            throw new BusinessRuleException("Only rejection requests are supported.");
        }
        if (complaint.StatusId != STATUS_ASSIGNED && complaint.StatusId != STATUS_IN_PROGRESS && complaint.StatusId != STATUS_ESCALATED)
        {
            throw new BusinessRuleException("Rejection request cannot be created for this complaint status.");
        }

        var existingRequest = await _requestRepository.GetPendingByComplaintAndTypeAsync(complaintId, REQUEST_TYPE_REJECTION);

        if (existingRequest != null)
        {
            throw new ConflictException("A pending rejection request already exists.");
        }

        var request = new ComplaintRequest
        {
            ComplaintId = complaintId,
            RequestTypeId = REQUEST_TYPE_REJECTION,
            RequestedBy = _currentUserService.EmployeeId,
            RequestStatusId = REQUEST_STATUS_PENDING,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        var createdRequest = await _requestRepository.Create(request);

        return await ReloadDto(createdRequest.RequestId);
    }

    public async Task<ComplaintRequestDto> ReviewAsync(int requestId, ReviewComplaintRequestDto dto)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        if (_currentUserService.RoleId != ROLE_ADMIN)
        {
            throw new ForbiddenException("Only admins can review complaint requests.");
        }

        var request = await _requestRepository.GetDetailAsync(requestId) ?? throw new NotFoundException($"Complaint request {requestId}");
        if (request.RequestTypeId != REQUEST_TYPE_REJECTION)
        {
            throw new BusinessRuleException("Only rejection requests can be reviewed.");
        }
        if (request.RequestStatusId != REQUEST_STATUS_PENDING)
        {
            throw new BusinessRuleException(
                "This request has already been reviewed.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            request.ReviewedBy = _currentUserService.EmployeeId;
            request.ReviewedAt = DateTime.UtcNow;
            request.RequestStatusId =
                dto.Approve
                    ? REQUEST_STATUS_APPROVED
                    : REQUEST_STATUS_REJECTED;

            request.Remarks = dto.Remarks ?? request.Remarks;

            await _requestRepository.Update(request, requestId);
            var complaint = await _complaintRepository.GetDetailByIdAsync(request.ComplaintId) ?? throw new NotFoundException($"Complaint {request.ComplaintId} Not found");
            var oldStatus = complaint.StatusId;
            // approve for rejection so complaint request is approved and complaint is rejected
            if (dto.Approve)
            {
                complaint.StatusId = STATUS_REJECTED;
                complaint.UpdatedAt = DateTime.UtcNow;

                await _complaintRepository.Update(complaint, complaint.ComplaintId);
                // after updating complaint history is created
                await _historyRepository.Create(
                                    new ComplaintHistory
                                    {
                                        ComplaintId = complaint.ComplaintId,
                                        OldStatusId = oldStatus,
                                        NewStatusId = STATUS_REJECTED,
                                        OldHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
                                        NewHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
                                        ChangedBy = _currentUserService.EmployeeId,
                                        Remarks = "Rejection request approved :" + dto.Remarks,
                                        EscalationLevelSnapshot = complaint.EscalationLevel,
                                        CreatedAt = DateTime.UtcNow,
                                        RoleIdAtActionTime = _currentUserService.RoleId
                                    });

                await _notificationService.SendAsync(
                    complaint.RaisedByEmployeeId,
                    NOTIF_COMPLAINT_REJECTED,
                    "Complaint Rejected",
                    $"Your complaint '{complaint.ComplaintTitle}' has been rejected.",
                    complaint.ComplaintId);
            }
            // not approved rejection is rejected so complaint request is rejected
            //  and complaint is reopened with priority bump to compensate for the delay caused
            else
            {
                if (complaint.ReopenedCount >= 3)
                    throw new BusinessRuleException(
                        "Cannot reopen complaint. Maximum reopen limit reached. Approve the rejection instead.");

                var oldhandler = complaint.CurrentHandlerEmployeeId;

                // priority bump
                if (complaint.PriorityId < 4)
                {
                    complaint.PriorityId += 1;
                }


                var raisedBy = complaint.RaisedByEmployee ?? throw new NotFoundException($"Employee {complaint.RaisedByEmployeeId} who raised the complaint");
                var assignment =
                   await _assignmentEngine
                       .DetermineInitialAssignmentAsync(
                           complaint.ComplaintId,
                           complaint.Category.DepartmentId,
                           raisedBy.RoleId);
                complaint.ReopenedCount++;

                complaint.StatusId = STATUS_REOPENED;
                // inserting status reopen history before changing handler to capture old handler in history
                await _historyRepository.Create(
                    new ComplaintHistory
                    {
                        ComplaintId = complaint.ComplaintId,
                        OldStatusId = oldStatus,
                        NewStatusId = STATUS_REOPENED,
                        OldHandlerEmployeeId = oldhandler,
                        NewHandlerEmployeeId = oldhandler,
                        ChangedBy = _currentUserService.EmployeeId,
                        RoleIdAtActionTime = _currentUserService.RoleId,
                        Remarks = "Rejection request rejected. Complaint reopened.",
                        EscalationLevelSnapshot = complaint.EscalationLevel,
                        CreatedAt = DateTime.UtcNow
                    });
                // after assignement new history and assignment history
                complaint.CurrentHandlerEmployeeId = assignment.HandlerId;

                complaint.StatusId = STATUS_ASSIGNED;

                complaint.EscalationLevel = assignment.EscalationLevel;

                complaint.EscalationDueAt = await _assignmentEngine.CalculateEscalationDueAtAsync(complaint);
                complaint.UpdatedAt =
                    DateTime.UtcNow;

                await _complaintRepository.Update(
                    complaint,
                    complaint.ComplaintId);

                await _assignmentRepository.Create(
                    new ComplaintAssignmentHistory
                    {
                        ComplaintId = complaint.ComplaintId,
                        OldHandlerEmployeeId = oldhandler,
                        NewHandlerEmployeeId = assignment.HandlerId,
                        AssignedBy = null,
                        AssignmentReason = "REOPEN"
                    });

                await _historyRepository.Create(
                    new ComplaintHistory
                    {
                        ComplaintId = complaint.ComplaintId,
                        OldStatusId = STATUS_REOPENED,
                        NewStatusId = STATUS_ASSIGNED,
                        OldHandlerEmployeeId = oldhandler,
                        NewHandlerEmployeeId = assignment.HandlerId,
                        ChangedBy = null,
                        RoleIdAtActionTime = null,
                        Remarks = "Complaint reassigned after reopen.",
                        EscalationLevelSnapshot = assignment.EscalationLevel,
                        CreatedAt = DateTime.UtcNow
                    });
                //notification for new handler
                await _notificationService.SendAsync(
assignment.HandlerId,
NOTIF_COMPLAINT_REOPENED,
"Complaint Assigned",
$"Complaint '{complaint.ComplaintTitle}' has been reopened and assigned to you.",
complaint.ComplaintId);
                //notification for complaint raiser
                await _notificationService.SendAsync(
                    complaint.RaisedByEmployeeId,
                    NOTIF_COMPLAINT_REOPENED,
                    "Complaint Reopened",
                    $"Your complaint '{complaint.ComplaintTitle}' has been reopened.",
                    complaint.ComplaintId);
            }

            await transaction.CommitAsync();

            return await ReloadDto(requestId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResultDto<ComplaintRequestDto>> GetPagedAsync(int page, int pageSize, short? statusId)
    {
        var (items, totalCount) =
            await _requestRepository.GetPagedAsync(
                page,
                pageSize,
                REQUEST_TYPE_REJECTION,
                statusId);

        return new PagedResultDto<ComplaintRequestDto>
        {
            Items = items
                .Select(MapToDto)
                .ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task<ComplaintRequestDto> ReloadDto(int requestId)
    {
        var request = await _requestRepository.GetDetailAsync(requestId)
            ?? throw new NotFoundException($"Complaint request {requestId} not found.");

        return MapToDto(request);
    }

    private static ComplaintRequestDto MapToDto(ComplaintRequest request)
    {
        return new ComplaintRequestDto
        {
            RequestId = request.RequestId,
            ComplaintId = request.ComplaintId,
            ComplaintTitle = request.Complaint?.ComplaintTitle,
            RequestTypeId = request.RequestTypeId,
            RequestTypeName = request.RequestType?.RequestTypeName ?? string.Empty,
            RequestedBy = request.RequestedBy,
            RequestedByName = request.RequestedByNavigation?.EmployeeName ?? string.Empty,
            ReviewedBy = request.ReviewedBy,
            ReviewedByName = request.ReviewedByNavigation?.EmployeeName,
            RequestStatusId = request.RequestStatusId,
            RequestStatusName = request.RequestStatus?.StatusName ?? string.Empty,
            Remarks = request.Remarks,
            CreatedAt = request.CreatedAt,
            ReviewedAt = request.ReviewedAt
        };
    }
}