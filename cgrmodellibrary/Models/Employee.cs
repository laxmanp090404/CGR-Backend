using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class Employee
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? MobileNumber { get; set; }

    public string? AddressEncrypted { get; set; }

    public string PasswordHash { get; set; } = null!;

    public int DepartmentId { get; set; }

    public short RoleId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Complaint> ComplaintAssignedToEmployees { get; set; } = new List<Complaint>();

    public virtual ICollection<ComplaintAttachment> ComplaintAttachments { get; set; } = new List<ComplaintAttachment>();

    public virtual ICollection<ComplaintComment> ComplaintComments { get; set; } = new List<ComplaintComment>();

    public virtual ICollection<ComplaintOfficer> ComplaintOfficerAssignedByNavigations { get; set; } = new List<ComplaintOfficer>();

    public virtual ICollection<ComplaintOfficer> ComplaintOfficerEmployees { get; set; } = new List<ComplaintOfficer>();

    public virtual ICollection<Complaint> ComplaintRaisedByEmployees { get; set; } = new List<Complaint>();

    public virtual ICollection<ComplaintStatusHistory> ComplaintStatusHistories { get; set; } = new List<ComplaintStatusHistory>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Escalation> EscalationAcknowledgedByNavigations { get; set; } = new List<Escalation>();

    public virtual ICollection<Escalation> EscalationEscalatedToEmployees { get; set; } = new List<Escalation>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<RoleRequest> RoleRequestApprovedEmployees { get; set; } = new List<RoleRequest>();

    public virtual ICollection<RoleRequest> RoleRequestReviewedByNavigations { get; set; } = new List<RoleRequest>();
}
