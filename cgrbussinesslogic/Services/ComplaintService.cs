using System.Net.Mail;
using cgrbussinesslogic.Helpers;
using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Attachment;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.Extensions.Logging;

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
    private readonly IComplaintEscalationRepository _escalationRepository;
    private readonly IComplaintAssignmentEngine _assignmentEngine;
    private readonly IEscalationRuleRepository _escalationRuleRepository;
    private readonly ILogger<ComplaintService> _logger;
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
        IComplaintEscalationRepository escalationRepository,
        IEscalationRuleRepository escalationRuleRepository,
        ILogger<ComplaintService> logger
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
        _logger = logger;
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
                .Select(ComplaintHelper.MapDashboardToDto)
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
        Complaint? complaint = null;
        try
        {
            var employeeId = _currentUserService.EmployeeId;

            var category = await _categoryRepository.Get(dto.CategoryId);

            if (!category.IsActive)
            {
                throw new BusinessRuleException("Category is inactive.");
            }
            //check for duplicate complaint in last 2 minutes with same title and description by same employee
            var duplicateExists =await _complaintRepository.ExistsRecentDuplicateAsync(_currentUserService.EmployeeId,dto.ComplaintTitle.Trim(),dto.ComplaintDescription.Trim(),TimeSpan.FromMinutes(2));

            if (duplicateExists)
            {
                throw new ConflictException(
                    "A similar complaint was recently submitted.");
            }
            // to avoid update and created at difference
            var utcnow = DateTime.UtcNow;
            complaint = new Complaint
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
            await ComplaintHelper.CreateHistoryAsync(_historyRepository, complaint.ComplaintId, null, STATUS_SUBMITTED, null, null, employeeId, _currentUserService.RoleId, "Complaint submitted.", 0);
            // Automatic assignment
            var assignment = await _assignmentEngine.DetermineInitialAssignmentAsync(
                    complaint.ComplaintId,
                    category.DepartmentId,
                    _currentUserService.RoleId);

            complaint.CurrentHandlerEmployeeId = assignment.HandlerId;
            complaint.StatusId = STATUS_ASSIGNED;
            complaint.EscalationLevel = assignment.EscalationLevel;

            // get escalation due of a complaint

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
            // notification for handler
            await _notificationService.SendAsync(
                                    assignment.HandlerId,
                                    NOTIF_COMPLAINT_ASSIGNED,
                                    "Complaint Assigned",
                                    $"Complaint '{complaint.ComplaintTitle}' has been assigned to you.",
                                    complaint.ComplaintId);
            // notification for creator of complaint
            await _notificationService.SendAsync(
                                    complaint.RaisedByEmployeeId,
                                    NOTIF_COMPLAINT_ASSIGNED,
                                    "Complaint Submitted",
                                    $"Your complaint '{complaint.ComplaintTitle}' has been submitted and assigned to {complaint.CurrentHandlerEmployee?.EmployeeName ?? $"id {complaint.CurrentHandlerEmployeeId}"}.",
                                    complaint.ComplaintId);
            // capture status change history for assignment in complaint history
            await ComplaintHelper.CreateHistoryAsync(
                    _historyRepository,
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

            _logger.LogInformation($"Complaint {complaint.ComplaintId} created by Employee {complaint.RaisedByEmployeeId}");
            return ComplaintHelper.MapToDto(complaint);
        }
        catch
        {
            await transaction.RollbackAsync();

            // await _attachmentService.DeleteFilesAsync(createdFiles);
            if (complaint?.ComplaintId > 0)
            {
                await _attachmentService.DeleteComplaintFolderAsync(
                    complaint.ComplaintId);
            }


            throw;
        }
    }
    //get complaint by id with details
    public async Task<ComplaintDto> GetByIdAsync(int complaintId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId} Not found");
        await ComplaintHelper.ValidateViewPermissionAsync(complaint, _currentUserService.EmployeeId, _currentUserService.RoleId, _currentUserService.DepartmentId);
        return ComplaintHelper.MapToDto(complaint);
    }

    // get the complaint history for a specific complaint
    public async Task<IEnumerable<ComplaintHistoryDto>> GetHistoryAsync(int complaintId)
    {
        var complaint =
            await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        await ComplaintHelper.ValidateViewPermissionAsync(complaint, _currentUserService.EmployeeId, _currentUserService.RoleId, _currentUserService.DepartmentId);

        var history =
            await _historyRepository.GetByComplaintIdAsync(complaintId);

        return history.Select(ComplaintHelper.MapHistoryToDto);
    }
    // change complaint status to in progress
    public async Task StartProgressAsync(int complaintId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
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

            await ComplaintHelper.CreateHistoryAsync(
                _historyRepository,
                complaint.ComplaintId,
                STATUS_ASSIGNED,
                STATUS_IN_PROGRESS,
                complaint.CurrentHandlerEmployeeId,
                complaint.CurrentHandlerEmployeeId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                "Complaint resolution progress started.",
                complaint.EscalationLevel);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    // resolve complaint
    public async Task ResolveAsync(int complaintId, ResolveComplaintDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
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

            await ComplaintHelper.CreateHistoryAsync(
                _historyRepository,
                complaint.ComplaintId,
                oldStatus,
                STATUS_RESOLVED,
                complaint.CurrentHandlerEmployeeId,
                complaint.CurrentHandlerEmployeeId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                dto.ResolutionRemarks,
                complaint.EscalationLevel);
            // notification for complaint creator
            await _notificationService.SendAsync(
                complaint.RaisedByEmployeeId,
                NOTIF_COMPLAINT_RESOLVED,
                "Complaint Resolved",
                $"Your complaint '{complaint.ComplaintTitle}' has been resolved.",
                complaint.ComplaintId);

            await transaction.CommitAsync();
            _logger.LogInformation($"Complaint {complaint.ComplaintId} resolved by Employee {_currentUserService.EmployeeId}");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    // close complaint
    public async Task CloseAsync(int complaintId, CloseComplaintDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
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

            await ComplaintHelper.CreateHistoryAsync(
                _historyRepository,
                complaint.ComplaintId,
                oldStatus,
                STATUS_CLOSED,
                complaint.CurrentHandlerEmployeeId,
                complaint.CurrentHandlerEmployeeId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                dto.Remarks,
                complaint.EscalationLevel);
            //notification for handler of complaint
            await _notificationService.SendAsync(
                complaint.CurrentHandlerEmployeeId!.Value,
                NOTIF_COMPLAINT_RESOLVED,
                "Complaint Closed",
                $"Complaint '{complaint.ComplaintTitle}' has been closed by the requester.",
                complaint.ComplaintId);

            await transaction.CommitAsync();
            _logger.LogInformation($"Complaint {complaint.ComplaintId} closed by Employee {_currentUserService.EmployeeId}");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

            await ComplaintHelper.CreateHistoryAsync(
                _historyRepository,
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

            await ComplaintHelper.CreateHistoryAsync(
                    _historyRepository,
                    complaint.ComplaintId,
                    STATUS_REOPENED,
                    STATUS_ASSIGNED,
                    oldHandler,
                    assignment.HandlerId,
                    null,
                    null,
                    "Complaint reassigned after reopen.",
                    assignment.EscalationLevel);
            // notification for new handler

            await _notificationService.SendAsync(
                assignment.HandlerId,
                NOTIF_COMPLAINT_REOPENED,
                "Complaint Reopened",
                $"Complaint '{complaint.ComplaintTitle}' has been reopened and assigned to you.",
                complaint.ComplaintId);
            // notifciagtion for complaint creator
            await _notificationService.SendAsync(
            complaint.RaisedByEmployeeId,
            NOTIF_COMPLAINT_REOPENED,
            "Complaint Reopened",
            $"Your complaint '{complaint.ComplaintTitle}' has been reopened.",
            complaint.ComplaintId);
            await transaction.CommitAsync();
            _logger.LogWarning($"Complaint {complaint.ComplaintId} reopened by Employee {_currentUserService.EmployeeId}");
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

            await ComplaintHelper.CreateHistoryAsync(
                _historyRepository,
                complaint.ComplaintId,
                oldStatus,
                STATUS_ESCALATED,
                oldHandler,
                newHandlerId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                dto.Remarks!,
                newEscalationLevel);
            // notification for new handler
            await _notificationService.SendAsync(
                    newHandlerId,
                    NOTIF_COMPLAINT_ESCALATED,
                    "Complaint Escalated",
                    $"Complaint '{complaint.ComplaintTitle}' has been escalated to you.",
                    complaint.ComplaintId);
            //notification for complaint creator
            await _notificationService.SendAsync(
            complaint.RaisedByEmployeeId,
            NOTIF_COMPLAINT_ESCALATED,
            "Complaint Escalated",
            $"Your complaint '{complaint.ComplaintTitle}' has been escalated.",
            complaint.ComplaintId);
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
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId) ?? throw new NotFoundException($"Complaint {complaintId}");
            var previoushandlers = await _assignmentRepository.GetPreviousHandlersAsync(complaintId);
            if (_currentUserService.RoleId != ROLE_ADMIN)
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

            await ComplaintHelper.CreateHistoryAsync(
                _historyRepository,
                complaint.ComplaintId,
                oldStatus,
                STATUS_ASSIGNED,
                oldHandlerId,
                dto.GroEmployeeId,
                _currentUserService.EmployeeId,
                _currentUserService.RoleId,
                dto.Remarks,
                complaint.EscalationLevel);
            // notification for new handler
            await _notificationService.SendAsync(
                dto.GroEmployeeId,
                NOTIF_COMPLAINT_ASSIGNED,
                "Complaint Assigned",
                $"Complaint '{complaint.ComplaintTitle}' has been assigned to you.",
                complaint.ComplaintId);
            //notification for complaint creator
            await _notificationService.SendAsync(
                complaint.RaisedByEmployeeId,
                NOTIF_COMPLAINT_ASSIGNED,
                "Complaint Reassigned",
                $"Your complaint '{complaint.ComplaintTitle}' has been reassigned.",
                complaint.ComplaintId);

            await transaction.CommitAsync();
            _logger.LogInformation($"Complaint {complaint.ComplaintId} assigned to Employee {dto.GroEmployeeId}");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<ComplaintAssignmentHistoryDto>> GetAssignmentHistoryAsync(int complaintId)
    {
        var complaint =
            await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException(
                $"Complaint {complaintId}");

        ComplaintHelper.ValidateHistoryAccessAsync(complaint, _currentUserService.EmployeeId, _currentUserService.RoleId, _currentUserService.DepartmentId);

        var history =
            await _assignmentRepository
                .GetByComplaintIdAsync(complaintId);

        return history.Select(h =>
            new ComplaintAssignmentHistoryDto
            {
                AssignmentHistoryId =
                    h.AssignmentHistoryId,

                ComplaintId =
                    h.ComplaintId,

                OldHandlerEmployeeId =
                    h.OldHandlerEmployeeId,

                OldHandlerEmployeeName =
                    h.OldHandlerEmployee?.EmployeeName,

                NewHandlerEmployeeId =
                    h.NewHandlerEmployeeId,

                NewHandlerEmployeeName =
                    h.NewHandlerEmployee.EmployeeName,

                AssignedBy =
                    h.AssignedBy,

                AssignedByName =
                    h.AssignedByNavigation?.EmployeeName,

                AssignmentReason =
                    h.AssignmentReason,

                AssignedAt =
                    h.AssignedAt
            });
    }

    public async Task<IEnumerable<ComplaintEscalationDto>> GetEscalationHistoryAsync(int complaintId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId} Not found");

        ComplaintHelper.ValidateHistoryAccessAsync(complaint, _currentUserService.EmployeeId, _currentUserService.RoleId, _currentUserService.DepartmentId);

        var escalations = await _escalationRepository.GetByComplaintIdAsync(complaintId);

        return escalations.Select(e => new ComplaintEscalationDto
        {
            EscalationId = e.EscalationId,
            ComplaintId = e.ComplaintId,
            EscalatedToEmployeeId = e.EscalatedToEmployeeId,
            EscalatedToEmployeeName = e.EscalatedToEmployee?.EmployeeName ?? "",
            EscalationLevel = e.EscalationLevel,
            EscalatedByEmployeeId = e.EscalatedByEmployeeId,
            EscalatedByEmployeeName = e.EscalatedByEmployee?.EmployeeName ?? "",
            EscalationRuleId = e.EscalationRuleId,
            Reason = e.Reason,
            EscalatedAt = e.EscalatedAt
        }).ToList();
    }
}