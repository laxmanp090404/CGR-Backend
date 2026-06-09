using System.Net.Mail;
using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Attachment;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;


public class ComplaintService : IComplaintService
{
    #region STATUS IDS
    private const short STATUS_SUBMITTED = 1;
    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_RESOLVED = 5;
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_REOPENED = 8;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;
    #endregion
    #region NOTIFICATION IDS
    private const short NOTIF_COMPLAINT_ASSIGNED = 3;
    private const short NOTIF_COMPLAINT_ESCALATED = 4;
    private const short NOTIF_COMPLAINT_RESOLVED = 5;
    private const short NOTIF_COMPLAINT_REJECTED = 6;
    private const short NOTIF_COMPLAINT_REOPENED = 7;
    private const short NOTIF_SLA_BREACH = 8;
    #endregion
    #region ROLE IDs
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;
    #endregion
    #region DECLARATIONS
    private readonly IComplaintRepository _complaintRepository;
    private readonly IComplaintHistoryRepository _historyRepository;
    private readonly IComplaintAssignmentRepository _assignmentRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IComplaintAttachmentService _attachmentService;
    private readonly IRepository<int, ComplaintEscalation> _escalationRepository;
    private readonly IComplaintAssignmentEngine _assignmentEngine;
    private readonly IEscalationRuleRepository _escalationRuleRepository;
    private readonly CGRContext _context;
    #endregion

    #region CONSTRUCTOR DEPENDENCY INJECTION
    public ComplaintService(
        IComplaintRepository complaintRepository,
        IComplaintHistoryRepository historyRepository,
        IComplaintAssignmentRepository assignmentRepository,
        ICategoryRepository categoryRepository,
        IEmployeeRepository employeeRepository,
        IComplaintAssignmentEngine assignmentEngine,
        INotificationService notificationService, CGRContext context,
        IComplaintAttachmentService attachmentService,
        ICurrentUserService currentUserService,
        IRepository<int, ComplaintEscalation> escalationRepository,
        IEscalationRuleRepository escalationRuleRepository
        )
    {
        _complaintRepository = complaintRepository;
        _historyRepository = historyRepository;
        _assignmentRepository = assignmentRepository;
        _categoryRepository = categoryRepository;
        _employeeRepository = employeeRepository;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _attachmentService = attachmentService;
        _escalationRepository = escalationRepository;
        _context = context;
        _assignmentEngine = assignmentEngine;
        _escalationRuleRepository = escalationRuleRepository;
    }
    #endregion

    // get complaints with pagination and filtering for dashboard
    public async Task<PagedResultDto<ComplaintDashboardDto>> GetPagedAsync(int page, int pageSize, int? statusId, int? priorityId, int? categoryId,
      int? departmentId, string? search)
    {
        var (items, totalCount) =
            await _complaintRepository
                .GetPagedDashboardAsync(page, pageSize, statusId, priorityId, categoryId, departmentId, search, _currentUserService.EmployeeId,
                    _currentUserService.Role, _currentUserService.DepartmentId);

        return new PagedResultDto<ComplaintDashboardDto>
        {
            Items = items
                .Select(MapDashboardToDto)
                .ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    //create complaints
    public async Task<ComplaintDto> CreateAsync(CreateComplaintDto dto)
    {
        if (_currentUserService.RoleId == ROLE_ADMIN)
        {
            throw new BusinessRuleException("Administrators cannot raise complaints.");
        }
        //transaction
        using var transaction = await _context.Database.BeginTransactionAsync();
        //delete files if any error occurs
        List<string> createdFiles = new();

        try
        {
            var employeeId = _currentUserService.EmployeeId;

            var category = await _categoryRepository.Get(dto.CategoryId);

            if (!category.IsActive)
            {
                throw new BusinessRuleException("Category is inactive.");
            }
            // to avoid update and created at difference
            var utcnow = DateTime.UtcNow;
            var complaint = new Complaint
            {
                ComplaintTitle = dto.ComplaintTitle,
                ComplaintDescription = dto.ComplaintDescription,
                RaisedByEmployeeId = employeeId,
                CategoryId = dto.CategoryId,
                PriorityId = category.DefaultPriorityId,
                StatusId = STATUS_SUBMITTED,
                EscalationLevel = 0,
                ReopenedCount = 0,
                CreatedAt = utcnow,
                UpdatedAt = utcnow
            };

            complaint = await _complaintRepository.Create(complaint);
            // CREATE COMPLAINT SUBMITTED HISTORY ALSO WHILE SUBMITTED STATE ESCALATION LEVEL IS 0
            await CreateHistoryAsync(complaint.ComplaintId, null, STATUS_SUBMITTED, null, null, employeeId, _currentUserService.RoleId, "Complaint submitted.", 0);
            // Automatic assignment
            var assignment = await _assignmentEngine.DetermineInitialAssignmentAsync(
                    complaint.ComplaintId,
                    category.DepartmentId,
                    _currentUserService.RoleId);

            complaint.CurrentHandlerEmployeeId = assignment.HandlerId;
            complaint.StatusId = STATUS_ASSIGNED;
            complaint.EscalationLevel = assignment.EscalationLevel;
            complaint.EscalationDueAt = await _assignmentEngine.CalculateEscalationDueAtAsync(complaint);
            complaint.UpdatedAt = DateTime.UtcNow;

            await _complaintRepository.Update(
                complaint,
                complaint.ComplaintId);
            // after assignment,assigned history created 
            //assigned by is null because auto assign
            await _assignmentRepository.Create(
                new ComplaintAssignmentHistory
                {
                    ComplaintId = complaint.ComplaintId,
                    OldHandlerEmployeeId = null,
                    NewHandlerEmployeeId = assignment.HandlerId,
                    AssignedBy = null,
                    AssignmentReason = "INITIAL",
                    AssignedAt = DateTime.UtcNow
                });
            // capture status change history for assignment in complaint history
            await CreateHistoryAsync(
                    complaint.ComplaintId,
                    STATUS_SUBMITTED,
                    STATUS_ASSIGNED,
                    null,
                    assignment.HandlerId,
                    null,
                    _currentUserService.RoleId,
                    "Complaint assigned.",
                    assignment.EscalationLevel);

            if (dto.Attachments != null &&
                dto.Attachments.Any())
            {
                var result = await _attachmentService.SaveAttachmentsAsync(complaint.ComplaintId, dto.Attachments);

                createdFiles = result.CreatedFiles;
            }

            await transaction.CommitAsync();

            complaint = await _complaintRepository.GetDetailByIdAsync(complaint.ComplaintId) ?? throw new Exception("Failed to reload complaint.");

            return MapToDto(complaint);
        }
        catch
        {
            await transaction.RollbackAsync();

            await _attachmentService.DeleteFilesAsync(createdFiles);

            throw;
        }
    }
    //get complaint by id with details
    public async Task<ComplaintDto> GetByIdAsync(int complaintId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId} Not found");
        await ValidateViewPermissionAsync(complaint);
        return MapToDto(complaint);
    }

    // get the complaint history for a specific complaint
    public async Task<IEnumerable<ComplaintHistoryDto>> GetHistoryAsync(int complaintId)
    {
        var complaint =
            await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        await ValidateViewPermissionAsync(complaint);

        var history =
            await _historyRepository.GetByComplaintIdAsync(complaintId);

        return history.Select(MapHistoryToDto);
    }
    // change complaint status to in progress
    public async Task StartProgressAsync(int complaintId)
    {
        var complaint =
            await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        if (complaint.CurrentHandlerEmployeeId != _currentUserService.EmployeeId)
        {
            throw new ForbiddenException("Only the current handler can start progress.");
        }

        if (complaint.StatusId != STATUS_ASSIGNED)
        {
            throw new BusinessRuleException("Only assigned complaints can be started progress.");
        }

        var oldStatus = complaint.StatusId;

        complaint.StatusId = STATUS_IN_PROGRESS;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _complaintRepository.Update(complaint, complaint.ComplaintId);

        await CreateHistoryAsync(
            complaint.ComplaintId,
            STATUS_ASSIGNED,
            STATUS_IN_PROGRESS,
            complaint.CurrentHandlerEmployeeId,
            complaint.CurrentHandlerEmployeeId,
            _currentUserService.EmployeeId,
            _currentUserService.RoleId,
            "Complaint resolution progress started.",
            complaint.EscalationLevel);
    }
    // resolve complaint
    public async Task ResolveAsync(int complaintId, ResolveComplaintDto dto)
    {
        var complaint =
            await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        if (complaint.CurrentHandlerEmployeeId != _currentUserService.EmployeeId)
        {
            throw new ForbiddenException("Only current handler can resolve.");
        }

        if (complaint.StatusId != STATUS_IN_PROGRESS)
        {
            throw new BusinessRuleException("Complaint is not in progress.");
        }

        var oldStatus = complaint.StatusId;

        complaint.StatusId = STATUS_RESOLVED;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _complaintRepository.Update(complaint, complaint.ComplaintId);

        await CreateHistoryAsync(
            complaint.ComplaintId,
            oldStatus,
            STATUS_RESOLVED,
            complaint.CurrentHandlerEmployeeId,
            complaint.CurrentHandlerEmployeeId,
            _currentUserService.EmployeeId,
            _currentUserService.RoleId,
            dto.ResolutionRemarks,
            complaint.EscalationLevel);
    }
    // close complaint
    public async Task CloseAsync(int complaintId, CloseComplaintDto dto)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        if (complaint.RaisedByEmployeeId != _currentUserService.EmployeeId)
        {
            throw new ForbiddenException("Only complaint creator can close a complaint.");
        }

        if (complaint.StatusId != STATUS_RESOLVED)
        {
            throw new BusinessRuleException("Only resolved complaints can be closed.");
        }

        var oldStatus = complaint.StatusId;

        complaint.StatusId = STATUS_CLOSED;
        complaint.ClosedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _complaintRepository.Update(complaint, complaint.ComplaintId);

        await CreateHistoryAsync(
            complaint.ComplaintId,
            oldStatus,
            STATUS_CLOSED,
            complaint.CurrentHandlerEmployeeId,
            complaint.CurrentHandlerEmployeeId,
            _currentUserService.EmployeeId,
            _currentUserService.RoleId,
            dto.Remarks,
            complaint.EscalationLevel);
    }

    public async Task ReopenAsync(int complaintId, ReopenComplaintDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId} Not found");

            if (complaint.RaisedByEmployeeId != _currentUserService.EmployeeId)
            {
                throw new ForbiddenException("Only complaint creator can reopen.");
            }

            if (complaint.StatusId != STATUS_RESOLVED &&
                complaint.StatusId != STATUS_EXTERNALLY_ESCALATED)
            {
                throw new BusinessRuleException("Only resolved or externally escalated complaints can be reopened.");
            }
            if (complaint.ReopenedCount >= 3)
            {
                throw new BusinessRuleException("Cannot reopen a complaint more than 3 times. Please raise a new complaint.");
            }
            var oldStatus = complaint.StatusId;
            var oldHandler = complaint.CurrentHandlerEmployeeId;

            complaint.StatusId = STATUS_REOPENED;
            complaint.ReopenedCount++;

            complaint.ResolvedAt = null;
            complaint.ClosedAt = null;

            complaint.UpdatedAt = DateTime.UtcNow;

            await _complaintRepository.Update(complaint, complaint.ComplaintId);

            await CreateHistoryAsync(
                complaint.ComplaintId,
                oldStatus,
                STATUS_REOPENED,
                complaint.CurrentHandlerEmployeeId,
                complaint.CurrentHandlerEmployeeId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                dto.ReopenRemarks,
                complaint.EscalationLevel);

            var assignment = await _assignmentEngine.DetermineInitialAssignmentAsync(complaint.ComplaintId, complaint.Category.DepartmentId, _currentUserService.RoleId);

            await _assignmentRepository.Create(
                new ComplaintAssignmentHistory
                {
                    ComplaintId = complaint.ComplaintId,
                    OldHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
                    NewHandlerEmployeeId = assignment.HandlerId,
                    AssignedBy = null,
                    AssignmentReason = "REOPEN"
                });

            complaint.CurrentHandlerEmployeeId = assignment.HandlerId;
            complaint.StatusId = STATUS_ASSIGNED;
            complaint.EscalationLevel = assignment.EscalationLevel;
            complaint.EscalationDueAt = await _assignmentEngine.CalculateEscalationDueAtAsync(complaint);
            complaint.UpdatedAt = DateTime.UtcNow;

            await _complaintRepository.Update(complaint, complaint.ComplaintId);

            await CreateHistoryAsync(
                    complaint.ComplaintId,
                    STATUS_REOPENED,
                    STATUS_ASSIGNED,
                    oldHandler,
                    assignment.HandlerId,
                    null,
                    null,
                    "Complaint reassigned after reopen.",
                    assignment.EscalationLevel);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task EscalateAsync(int complaintId, EscalateComplaintDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId} Not found");

            if (complaint.CurrentHandlerEmployeeId != _currentUserService.EmployeeId)
            {
                throw new ForbiddenException("Only current handler can escalate.");
            }

            if (complaint.StatusId != STATUS_IN_PROGRESS)
            {
                throw new BusinessRuleException("Only in-progress complaints can be escalated.");
            }

            var currentRole = _currentUserService.RoleId;

            int newHandlerId;
            short newEscalationLevel;

            if (currentRole == ROLE_GRO)
            {
                var departmentHead = await _employeeRepository.GetDepartmentHeadAsync(complaint.Category.DepartmentId);

                if (departmentHead == null)
                {
                    throw new BusinessRuleException("No Department Head found.");
                }

                newHandlerId = departmentHead.EmployeeId;
                newEscalationLevel = 1;
            }
            else if (currentRole == ROLE_DEPARTMENT_HEAD)
            {
                var admin = await _employeeRepository.GetAdminAsync() ?? throw new BusinessRuleException("No active admin found.");

                newHandlerId = admin.EmployeeId;
                newEscalationLevel = 2;
            }
            else
            {
                throw new BusinessRuleException("Admin cannot escalate complaints.");
            }

            var oldStatus = complaint.StatusId;
            var oldHandler = complaint.CurrentHandlerEmployeeId;

            complaint.CurrentHandlerEmployeeId = newHandlerId;
            complaint.StatusId = STATUS_ESCALATED;
            complaint.EscalationLevel = newEscalationLevel;
            complaint.EscalationDueAt = await _assignmentEngine.CalculateEscalationDueAtAsync(complaint);
            complaint.UpdatedAt = DateTime.UtcNow;

            await _complaintRepository.Update(
                complaint,
                complaint.ComplaintId);

            await _escalationRepository.Create(
                new ComplaintEscalation
                {
                    ComplaintId = complaint.ComplaintId,
                    EscalatedToEmployeeId = newHandlerId,
                    EscalationLevel = newEscalationLevel,
                    EscalatedByEmployeeId = _currentUserService.EmployeeId,
                    Reason = dto.Remarks,
                    EscalatedAt = DateTime.UtcNow
                });

            await _assignmentRepository.Create(
                new ComplaintAssignmentHistory
                {
                    ComplaintId = complaint.ComplaintId,
                    OldHandlerEmployeeId = oldHandler,
                    NewHandlerEmployeeId = newHandlerId,
                    AssignedBy = _currentUserService.EmployeeId,
                    AssignmentReason = "ESCALATION"
                });

            await CreateHistoryAsync(
                complaint.ComplaintId,
                oldStatus,
                STATUS_ESCALATED,
                oldHandler,
                newHandlerId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                dto.Remarks!,
                newEscalationLevel);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task AssignAsync(int complaintId, AssignComplaintDto dto)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId}");
        var previoushandlers = await _assignmentRepository.GetPreviousHandlersAsync(complaintId);
        if (_currentUserService.Role != "ADMIN")
        {
            throw new ForbiddenException("Only admins can manually assign complaints.");
        }

        if (complaint.StatusId == STATUS_RESOLVED ||
            complaint.StatusId == STATUS_REJECTED ||
            complaint.StatusId == STATUS_CLOSED ||
            complaint.StatusId == STATUS_EXTERNALLY_ESCALATED)
        {
            throw new BusinessRuleException(
                "Complaint cannot be manually assigned in its current status.");
        }

        var gro =
            await _employeeRepository.GetByIdWithRoleAsync(dto.GroEmployeeId);

        if (gro == null)
        {
            throw new NotFoundException($"Employee {dto.GroEmployeeId} Not found");
        }

        if (!gro.IsActive)
        {
            throw new BusinessRuleException("Employee is inactive.");
        }

        if (gro.Role?.RoleName != "GRO")
        {
            throw new BusinessRuleException(
                "Complaint can only be assigned to a GRO.");
        }
        if (gro.DepartmentId != complaint.Category.DepartmentId)
        {
            throw new BusinessRuleException("Complaint can only be assigned to an employee in the same department.");
        }
        if (previoushandlers.Contains(gro.EmployeeId))
        {
            throw new BusinessRuleException("Cannot assign to a handler who has already handled this complaint.");
        }

        var oldHandlerId = complaint.CurrentHandlerEmployeeId;
        var oldStatus = complaint.StatusId;
        complaint.CurrentHandlerEmployeeId = dto.GroEmployeeId;
        complaint.EscalationLevel = 0;
        complaint.EscalationDueAt = await _assignmentEngine.CalculateEscalationDueAtAsync(complaint);
        complaint.StatusId = STATUS_ASSIGNED;
        complaint.UpdatedAt = DateTime.UtcNow;

        await _complaintRepository.Update(
            complaint,
            complaint.ComplaintId);

        await _assignmentRepository.Create(
            new ComplaintAssignmentHistory
            {
                ComplaintId = complaint.ComplaintId,
                OldHandlerEmployeeId = oldHandlerId,
                NewHandlerEmployeeId = dto.GroEmployeeId,
                AssignedBy = _currentUserService.EmployeeId,
                AssignmentReason = "MANUAL"
            });

        await CreateHistoryAsync(
            complaint.ComplaintId,
            oldStatus,
            STATUS_ASSIGNED,
            oldHandlerId,
            dto.GroEmployeeId,
            _currentUserService.EmployeeId,
            _currentUserService.RoleId,
            dto.Remarks,
            complaint.EscalationLevel);
    }
    #region HELPERS

    private static ComplaintDto MapToDto(Complaint c) => new()
    {
        ComplaintId = c.ComplaintId,
        ComplaintTitle = c.ComplaintTitle,
        ComplaintDescription = c.ComplaintDescription,
        RaisedByEmployeeId = c.RaisedByEmployeeId,
        RaisedByEmployeeName = c.RaisedByEmployee?.EmployeeName ?? string.Empty,
        CurrentHandlerEmployeeId = c.CurrentHandlerEmployeeId,
        CurrentHandlerEmployeeName = c.CurrentHandlerEmployee?.EmployeeName,
        CategoryId = c.CategoryId,
        CategoryName = c.Category?.CategoryName ?? string.Empty,
        DepartmentId = c.Category?.DepartmentId ?? 0,
        DepartmentName = c.Category?.Department?.DepartmentName ?? string.Empty,
        PriorityId = c.PriorityId,
        PriorityName = c.Priority?.PriorityName ?? string.Empty,
        StatusId = c.StatusId,
        StatusName = c.Status?.StatusName ?? string.Empty,
        EscalationLevel = c.EscalationLevel,
        ReopenedCount = c.ReopenedCount,
        EscalationDueAt = c.EscalationDueAt,
        Attachments = c.ComplaintAttachments
                    .Select(a => new ComplaintAttachmentDto
                    {
                        ComplaintId = c.ComplaintId,
                        AttachmentId = a.AttachmentId,
                        OriginalFileName = a.OriginalFileName,
                        FilePath = a.FilePath,
                        MimeType = a.MimeType,
                        FileSizeBytes = a.FileSizeBytes,
                        UploadedBy = a.UploadedBy,
                        UploadedByName = a.UploadedByNavigation?.EmployeeName ?? string.Empty,
                        CreatedAt = a.CreatedAt
                    })
                    .ToList(),
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        ResolvedAt = c.ResolvedAt,
        ClosedAt = c.ClosedAt
    };

    private static ComplaintDashboardDto MapDashboardToDto(VComplaintDashboard v) => new()
    {
        ComplaintId = v.ComplaintId ?? 0,
        ComplaintTitle = v.ComplaintTitle,
        StatusName = v.StatusName,
        PriorityName = v.PriorityName,
        CategoryName = v.CategoryName,
        DepartmentName = v.DepartmentName,
        RaisedByName = v.RaisedByName,
        CurrentHandlerName = v.CurrentHandlerName,
        EscalationLevel = v.EscalationLevel ?? 0,
        EscalationDueAt = v.EscalationDueAt,
        CreatedAt = v.CreatedAt ?? DateTime.MinValue,
        UpdatedAt = v.UpdatedAt ?? DateTime.MinValue,
        ClosedAt = v.ClosedAt
    };

    private static ComplaintHistoryDto MapHistoryToDto(ComplaintHistory h) => new()
    {
        HistoryId = h.HistoryId,
        ComplaintId = h.ComplaintId,
        OldStatusId = h.OldStatusId,
        OldStatusName = h.OldStatus?.StatusName ?? string.Empty,
        NewStatusId = h.NewStatusId,
        NewStatusName = h.NewStatus?.StatusName ?? string.Empty,
        OldHandlerEmployeeId = h.OldHandlerEmployeeId,
        OldHandlerName = h.OldHandlerEmployee?.EmployeeName,
        NewHandlerEmployeeId = h.NewHandlerEmployeeId,
        NewHandlerName = h.NewHandlerEmployee?.EmployeeName,
        Remarks = h.Remarks,
        ChangedBy = h.ChangedBy,
        ChangedByName = h.ChangedByNavigation?.EmployeeName,
        RoleIdAtActionTime = h.RoleIdAtActionTime,
        RoleNameAtActionTime = h.RoleIdAtActionTimeNavigation?.RoleName ?? string.Empty,
        CreatedAt = h.CreatedAt
    };

    private async Task ValidateViewPermissionAsync(Complaint complaint)
    {
        var employeeId = _currentUserService.EmployeeId;
        var role = _currentUserService.RoleId;
        // admin can see all
        if (role == ROLE_ADMIN)
        {
            return;
        }
        // raised by can see 
        if (complaint.RaisedByEmployeeId == employeeId)
        {
            return;
        }
        //handler can see
        if (complaint.CurrentHandlerEmployeeId == employeeId)
        {
            return;
        }
        // can see his dept complaints
        if (role == ROLE_DEPARTMENT_HEAD)
        {

            if (_currentUserService.DepartmentId == complaint.Category.DepartmentId)
            {
                return;
            }
        }

        throw new ForbiddenException("You are not authorized to view this complaint.");
    }
    private async Task CreateHistoryAsync(int complaintId, short? oldStatusId, short newStatusId, int? oldHandlerId, int? newHandlerId, int? changedBy, short? roleIdAtActionTime, string remarks, short escalationLevel)
    {
        await _historyRepository.Create(
            new ComplaintHistory
            {
                ComplaintId = complaintId,
                OldStatusId = oldStatusId,
                NewStatusId = newStatusId,
                EscalationLevelSnapshot = escalationLevel,
                OldHandlerEmployeeId = oldHandlerId,
                NewHandlerEmployeeId = newHandlerId,
                ChangedBy = changedBy,
                RoleIdAtActionTime = roleIdAtActionTime,
                Remarks = remarks,
                CreatedAt = DateTime.UtcNow
            });
    }

    #endregion
}