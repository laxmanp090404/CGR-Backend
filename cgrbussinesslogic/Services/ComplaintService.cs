using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

/// <summary>
/// Status IDs (from seed): SUBMITTED=1, ASSIGNED=2, IN_PROGRESS=3, ESCALATED=4,
/// RESOLVED=5, CLOSED=6, REJECTED=7, REOPENED=8, EXTERNALLY_ESCALATED=9
/// </summary>
public class ComplaintService : IComplaintService
{
    private const short STATUS_SUBMITTED = 1;
    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_RESOLVED = 5;
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_REOPENED = 8;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;

    // Notification type IDs (from seed)
    private const short NOTIF_COMPLAINT_ASSIGNED = 3;
    private const short NOTIF_COMPLAINT_ESCALATED = 4;
    private const short NOTIF_COMPLAINT_RESOLVED = 5;
    private const short NOTIF_COMPLAINT_REJECTED = 6;
    private const short NOTIF_COMPLAINT_REOPENED = 7;
    private const short NOTIF_SLA_BREACH = 8;

    private readonly IComplaintRepository _complaintRepository;
    private readonly IComplaintHistoryRepository _historyRepository;
    private readonly IComplaintAssignmentRepository _assignmentRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEscalationRuleRepository _escalationRuleRepository;
    private readonly INotificationService _notificationService;

    public ComplaintService(
        IComplaintRepository complaintRepository,
        IComplaintHistoryRepository historyRepository,
        IComplaintAssignmentRepository assignmentRepository,
        ICategoryRepository categoryRepository,
        IEmployeeRepository employeeRepository,
        IEscalationRuleRepository escalationRuleRepository,
        INotificationService notificationService)
    {
        _complaintRepository = complaintRepository;
        _historyRepository = historyRepository;
        _assignmentRepository = assignmentRepository;
        _categoryRepository = categoryRepository;
        _employeeRepository = employeeRepository;
        _escalationRuleRepository = escalationRuleRepository;
        _notificationService = notificationService;
    }

    public async Task<ComplaintDto> CreateAsync(CreateComplaintDto dto, int employeeId)
    {
        var category = await _categoryRepository.Get(dto.CategoryId);
        if (!category.IsActive)
            throw new BusinessRuleException("The selected category is inactive.");

        // Auto-assign GRO with least workload in that department
        var workloads = await _employeeRepository.GetGroActiveWorkloadAsync(category.DepartmentId);
        var previousHandlers = new List<int>();

        var assignedGro = workloads
            .Where(w => !previousHandlers.Contains(w.EmployeeId!.Value))
            .FirstOrDefault();

        var now = DateTime.UtcNow;
        var complaint = new Complaint
        {
            ComplaintTitle = dto.ComplaintTitle,
            ComplaintDescription = dto.ComplaintDescription,
            RaisedByEmployeeId = employeeId,
            CategoryId = dto.CategoryId,
            PriorityId = category.DefaultPriorityId,
            StatusId = assignedGro != null ? STATUS_ASSIGNED : STATUS_SUBMITTED,
            CurrentHandlerEmployeeId = assignedGro?.EmployeeId,
            EscalationLevel = 0,
            ReopenedCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _complaintRepository.Create(complaint);

        // Record assignment history if assigned
        if (assignedGro != null)
        {
            await _assignmentRepository.Create(new ComplaintAssignmentHistory
            {
                ComplaintId = created.ComplaintId,
                OldHandlerEmployeeId = null,
                NewHandlerEmployeeId = assignedGro.EmployeeId!.Value,
                AssignedBy = employeeId
            });

            await _notificationService.SendAsync(
                assignedGro.EmployeeId!.Value, NOTIF_COMPLAINT_ASSIGNED,
                "New Complaint Assigned",
                $"Complaint '{dto.ComplaintTitle}' has been assigned to you.",
                created.ComplaintId);
        }

        // Record status history
        await _historyRepository.Create(new ComplaintHistory
        {
            ComplaintId = created.ComplaintId,
            OldStatusId = null,
            NewStatusId = created.StatusId,
            OldHandlerEmployeeId = null,
            NewHandlerEmployeeId = created.CurrentHandlerEmployeeId,
            ChangedBy = employeeId,
            Remarks = "Complaint submitted.",
            CreatedAt = now,
            EscalationLevelSnapshot = 0
        });

        return await GetComplaintDtoAsync(created.ComplaintId);
    }

    public async Task<ComplaintDto> GetByIdAsync(int complaintId, int currentEmployeeId, string role)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");
        AssertCanView(complaint, currentEmployeeId, role);
        return MapToDto(complaint);
    }

    public async Task<PagedResultDto<ComplaintDashboardDto>> GetPagedAsync(
        int page, int pageSize, int? statusId, int? priorityId, int? categoryId, int? departmentId, string? search,
        int currentEmployeeId, string role, int? currentDepartmentId)
    {
        var (items, total) = await _complaintRepository.GetPagedDashboardAsync(
            page, pageSize, statusId, priorityId, categoryId, departmentId, search,
            currentEmployeeId, role, currentDepartmentId);

        return new PagedResultDto<ComplaintDashboardDto>
        {
            Items = items.Select(MapDashboardToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ComplaintHistoryDto>> GetHistoryAsync(int complaintId, int currentEmployeeId, string role)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");
        AssertCanView(complaint, currentEmployeeId, role);

        var history = await _historyRepository.GetByComplaintIdAsync(complaintId);
        return history.Select(MapHistoryToDto);
    }

    public async Task StartProgressAsync(int complaintId, int groId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        if (complaint.CurrentHandlerEmployeeId != groId)
            throw new ForbiddenException("You are not the assigned handler of this complaint.");

        if (complaint.StatusId != STATUS_ASSIGNED && complaint.StatusId != STATUS_REOPENED)
            throw new BusinessRuleException($"Cannot start progress on a complaint in '{complaint.Status?.StatusName}' status.");

        var oldStatusId = complaint.StatusId;
        complaint.StatusId = STATUS_IN_PROGRESS;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _complaintRepository.Update(complaint, complaintId);

        await _historyRepository.Create(new ComplaintHistory
        {
            ComplaintId = complaintId,
            OldStatusId = oldStatusId,
            NewStatusId = STATUS_IN_PROGRESS,
            OldHandlerEmployeeId = groId,
            NewHandlerEmployeeId = groId,
            ChangedBy = groId,
            Remarks = "GRO started working on the complaint.",
            CreatedAt = DateTime.UtcNow,
            EscalationLevelSnapshot = complaint.EscalationLevel
        });
    }

    public async Task ResolveAsync(int complaintId, int groId, ResolveComplaintDto dto)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        if (complaint.CurrentHandlerEmployeeId != groId)
            throw new ForbiddenException("You are not the assigned handler of this complaint.");

        if (complaint.StatusId != STATUS_IN_PROGRESS)
            throw new BusinessRuleException($"Cannot resolve a complaint in '{complaint.Status?.StatusName}' status.");

        var oldStatusId = complaint.StatusId;
        complaint.StatusId = STATUS_RESOLVED;
        complaint.ResolvedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _complaintRepository.Update(complaint, complaintId);

        await _historyRepository.Create(new ComplaintHistory
        {
            ComplaintId = complaintId,
            OldStatusId = oldStatusId,
            NewStatusId = STATUS_RESOLVED,
            OldHandlerEmployeeId = groId,
            NewHandlerEmployeeId = groId,
            ChangedBy = groId,
            Remarks = dto.ResolutionRemarks,
            CreatedAt = DateTime.UtcNow,
            EscalationLevelSnapshot = complaint.EscalationLevel
        });

        await _notificationService.SendAsync(
            complaint.RaisedByEmployeeId, NOTIF_COMPLAINT_RESOLVED,
            "Complaint Resolved",
            $"Your complaint '{complaint.ComplaintTitle}' has been resolved. Please review and close or reopen.",
            complaintId);
    }

    public async Task CloseAsync(int complaintId, int employeeId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        if (complaint.RaisedByEmployeeId != employeeId)
            throw new ForbiddenException("Only the complainant can close this complaint.");

        if (complaint.StatusId != STATUS_RESOLVED)
            throw new BusinessRuleException($"Cannot close a complaint in '{complaint.Status?.StatusName}' status.");

        var oldStatusId = complaint.StatusId;
        complaint.StatusId = STATUS_CLOSED;
        complaint.ClosedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _complaintRepository.Update(complaint, complaintId);

        await _historyRepository.Create(new ComplaintHistory
        {
            ComplaintId = complaintId,
            OldStatusId = oldStatusId,
            NewStatusId = STATUS_CLOSED,
            OldHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
            NewHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
            ChangedBy = employeeId,
            Remarks = "Complainant accepted the resolution and closed the complaint.",
            CreatedAt = DateTime.UtcNow,
            EscalationLevelSnapshot = complaint.EscalationLevel
        });
    }

    public async Task ReopenAsync(int complaintId, int employeeId, ReopenComplaintDto dto)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        if (complaint.RaisedByEmployeeId != employeeId)
            throw new ForbiddenException("Only the complainant can reopen this complaint.");

        if (complaint.StatusId != STATUS_RESOLVED && complaint.StatusId != STATUS_EXTERNALLY_ESCALATED)
            throw new BusinessRuleException($"Cannot reopen a complaint in '{complaint.Status?.StatusName}' status.");

        // Priority bump on reopen
        var newPriorityId = (short)Math.Min(complaint.PriorityId + 1, 4);

        // Exclude all previous handlers for reassignment
        var previousHandlers = await _assignmentRepository.GetPreviousHandlersAsync(complaintId);
        var workloads = await _employeeRepository.GetGroActiveWorkloadAsync(complaint.Category?.DepartmentId);

        var newGro = workloads
            .Where(w => !previousHandlers.Contains(w.EmployeeId!.Value))
            .FirstOrDefault();

        var oldStatusId = complaint.StatusId;
        var oldHandlerId = complaint.CurrentHandlerEmployeeId;

        complaint.StatusId = STATUS_REOPENED;
        complaint.PriorityId = newPriorityId;
        complaint.ReopenedCount = (short)(complaint.ReopenedCount + 1);
        complaint.CurrentHandlerEmployeeId = newGro?.EmployeeId ?? oldHandlerId;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _complaintRepository.Update(complaint, complaintId);

        if (newGro != null)
        {
            await _assignmentRepository.Create(new ComplaintAssignmentHistory
            {
                ComplaintId = complaintId,
                OldHandlerEmployeeId = oldHandlerId,
                NewHandlerEmployeeId = newGro.EmployeeId!.Value,
                AssignedBy = employeeId
            });
        }

        await _historyRepository.Create(new ComplaintHistory
        {
            ComplaintId = complaintId,
            OldStatusId = oldStatusId,
            NewStatusId = STATUS_REOPENED,
            OldHandlerEmployeeId = oldHandlerId,
            NewHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
            ChangedBy = employeeId,
            Remarks = dto.ReopenRemarks,
            CreatedAt = DateTime.UtcNow,
            EscalationLevelSnapshot = complaint.EscalationLevel
        });

        if (newGro != null)
        {
            await _notificationService.SendAsync(
                newGro.EmployeeId!.Value, NOTIF_COMPLAINT_REOPENED,
                "Complaint Reopened and Reassigned",
                $"Complaint '{complaint.ComplaintTitle}' has been reopened and assigned to you.",
                complaintId);
        }

        await _notificationService.SendAsync(
            employeeId, NOTIF_COMPLAINT_REOPENED,
            "Complaint Reopened",
            $"Your complaint '{complaint.ComplaintTitle}' has been reopened with bumped priority.",
            complaintId);
    }

    public async Task EscalateAsync(int complaintId, int currentEmployeeId, string role, EscalateComplaintDto dto)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        if (complaint.CurrentHandlerEmployeeId != currentEmployeeId && role != "ADMIN")
            throw new ForbiddenException("You are not authorized to escalate this complaint.");

        var allowedStatuses = new short[] { STATUS_ASSIGNED, STATUS_IN_PROGRESS, STATUS_REOPENED, STATUS_ESCALATED };
        if (!allowedStatuses.Contains(complaint.StatusId))
            throw new BusinessRuleException($"Cannot escalate a complaint in '{complaint.Status?.StatusName}' status.");

        var nextLevel = (short)(complaint.EscalationLevel + 1);
        var rule = await _escalationRuleRepository.GetRuleAsync(complaint.CategoryId, complaint.PriorityId, nextLevel);

        short newStatusId;
        if (rule == null)
        {
            // No more escalation rules — externally escalated
            newStatusId = STATUS_EXTERNALLY_ESCALATED;
        }
        else
        {
            newStatusId = STATUS_ESCALATED;
        }

        var oldStatusId = complaint.StatusId;
        var oldHandlerId = complaint.CurrentHandlerEmployeeId;

        complaint.EscalationLevel = nextLevel;
        complaint.StatusId = newStatusId;
        complaint.UpdatedAt = DateTime.UtcNow;
        await _complaintRepository.Update(complaint, complaintId);

        await _historyRepository.Create(new ComplaintHistory
        {
            ComplaintId = complaintId,
            OldStatusId = oldStatusId,
            NewStatusId = newStatusId,
            OldHandlerEmployeeId = oldHandlerId,
            NewHandlerEmployeeId = complaint.CurrentHandlerEmployeeId,
            ChangedBy = currentEmployeeId,
            Remarks = dto.Remarks ?? "Complaint escalated.",
            CreatedAt = DateTime.UtcNow,
            EscalationLevelSnapshot = nextLevel
        });

        await _notificationService.SendAsync(
            complaint.RaisedByEmployeeId, NOTIF_COMPLAINT_ESCALATED,
            "Complaint Escalated",
            $"Your complaint '{complaint.ComplaintTitle}' has been escalated to level {nextLevel}.",
            complaintId);
    }

    // ─── Private Helpers ────────────────────────────────────────────────────────

    private async Task<ComplaintDto> GetComplaintDtoAsync(int complaintId)
    {
        var c = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");
        return MapToDto(c);
    }

    private static void AssertCanView(Complaint c, int employeeId, string role)
    {
        if (role == "ADMIN") return;
        if (c.RaisedByEmployeeId == employeeId) return;
        if (role == "GRO" && c.CurrentHandlerEmployeeId == employeeId) return;
        if (role == "DEPARTMENT_HEAD") return; // DeptHead can see all in their dept
        throw new ForbiddenException("You are not authorized to view this complaint.");
    }

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
        UpdatedAt = v.UpdatedAt ?? DateTime.MinValue
    };

    private static ComplaintHistoryDto MapHistoryToDto(ComplaintHistory h) => new()
    {
        HistoryId = h.HistoryId,
        ComplaintId = h.ComplaintId,
        OldStatusId = h.OldStatusId,
        OldStatusName = h.OldStatus?.StatusName,
        NewStatusId = h.NewStatusId,
        NewStatusName = h.NewStatus?.StatusName ?? string.Empty,
        OldHandlerEmployeeId = h.OldHandlerEmployeeId,
        OldHandlerName = h.OldHandlerEmployee?.EmployeeName,
        NewHandlerEmployeeId = h.NewHandlerEmployeeId,
        NewHandlerName = h.NewHandlerEmployee?.EmployeeName,
        Remarks = h.Remarks,
        ChangedBy = h.ChangedBy,
        ChangedByName = h.ChangedByNavigation?.EmployeeName,
        CreatedAt = h.CreatedAt
    };
}
