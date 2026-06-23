using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Attachment;
using cgrmodellibrary.DTOs.Complaint;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Helpers;

public class ComplaintHelper
{
    private const short ROLE_ADMIN = 4;
    private const short ROLE_DEPARTMENT_HEAD = 3;

    public static async Task ValidateViewPermissionAsync(Complaint complaint, int employeeId, short role, int? departmentId)
    {
        if (role == ROLE_ADMIN)
        {
            return;
        }
        if (complaint.RaisedByEmployeeId == employeeId)
        {
            return;
        }
        if (complaint.CurrentHandlerEmployeeId == employeeId)
        {
            return;
        }
        if (role == ROLE_DEPARTMENT_HEAD)
        {
            if (departmentId == complaint.Category.DepartmentId)
            {
                return;
            }
        }

        throw new ForbiddenException("You are not authorized to view this complaint.");
    }

    public static async Task CreateHistoryAsync(IComplaintHistoryRepository historyRepository, int complaintId, short? oldStatusId, short newStatusId, int? oldHandlerId, int? newHandlerId, int? changedBy, short? roleIdAtActionTime, string remarks, short escalationLevel)
    {
        await historyRepository.Create(
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

   public static void ValidateHistoryAccessAsync(
    Complaint complaint,
    int employeeId,
    short roleId,
    int? departmentId)
{
    if (roleId == ROLE_ADMIN)
        return;

    if (complaint.RaisedByEmployeeId == employeeId)
        return;

    if (complaint.CurrentHandlerEmployeeId == employeeId)
        return;

    if (roleId == ROLE_DEPARTMENT_HEAD &&
        departmentId == complaint.Category.DepartmentId)
        return;

    throw new ForbiddenException(
        "You do not have access to this complaint.");
}
    #region  Mappers

    public static ComplaintDto MapToDto(Complaint c) => new()
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

    public static ComplaintDashboardDto MapDashboardToDto(VComplaintDashboard v) => new()
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

    public static ComplaintHistoryDto MapHistoryToDto(ComplaintHistory h) => new()
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
        ChangedByName = h.ChangedByNavigation?.EmployeeName ?? "System",
        RoleIdAtActionTime = h.RoleIdAtActionTime,
        RoleNameAtActionTime = h.RoleIdAtActionTimeNavigation?.RoleName ?? "System",
        CreatedAt = h.CreatedAt
    };

    #endregion
}
