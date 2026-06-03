using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Employee
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public short RoleId { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<ComplaintAssignmentHistory> ComplaintAssignmentHistoryAssignedByNavigations { get; set; } = new List<ComplaintAssignmentHistory>();

    public virtual ICollection<ComplaintAssignmentHistory> ComplaintAssignmentHistoryNewHandlerEmployees { get; set; } = new List<ComplaintAssignmentHistory>();

    public virtual ICollection<ComplaintAssignmentHistory> ComplaintAssignmentHistoryOldHandlerEmployees { get; set; } = new List<ComplaintAssignmentHistory>();

    public virtual ICollection<ComplaintAttachment> ComplaintAttachments { get; set; } = new List<ComplaintAttachment>();

    public virtual ICollection<ComplaintComment> ComplaintComments { get; set; } = new List<ComplaintComment>();

    public virtual ICollection<Complaint> ComplaintCurrentHandlerEmployees { get; set; } = new List<Complaint>();

    public virtual ICollection<ComplaintEscalation> ComplaintEscalationEscalatedByEmployees { get; set; } = new List<ComplaintEscalation>();

    public virtual ICollection<ComplaintEscalation> ComplaintEscalationEscalatedToEmployees { get; set; } = new List<ComplaintEscalation>();

    public virtual ICollection<ComplaintHistory> ComplaintHistoryChangedByNavigations { get; set; } = new List<ComplaintHistory>();

    public virtual ICollection<ComplaintHistory> ComplaintHistoryNewHandlerEmployees { get; set; } = new List<ComplaintHistory>();

    public virtual ICollection<ComplaintHistory> ComplaintHistoryOldHandlerEmployees { get; set; } = new List<ComplaintHistory>();

    public virtual ICollection<Complaint> ComplaintRaisedByEmployees { get; set; } = new List<Complaint>();

    public virtual ICollection<ComplaintRequest> ComplaintRequestRequestedByNavigations { get; set; } = new List<ComplaintRequest>();

    public virtual ICollection<ComplaintRequest> ComplaintRequestReviewedByNavigations { get; set; } = new List<ComplaintRequest>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Role Role { get; set; } = null!;

public virtual ICollection<RoleRequest> RoleRequests { get; set; } = new List<RoleRequest>();
    public virtual ICollection<RoleRequest> RoleRequestReviewedByNavigations { get; set; } = new List<RoleRequest>();
}
