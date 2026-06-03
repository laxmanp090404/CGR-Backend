using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using cgrmodellibrary.Models;

namespace cgrdataaccesslibrary.Context;

public partial class CGRContext : DbContext
{
    public CGRContext(DbContextOptions<CGRContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<ComplaintAssignmentHistory> ComplaintAssignmentHistories { get; set; }

    public virtual DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }

    public virtual DbSet<ComplaintComment> ComplaintComments { get; set; }

    public virtual DbSet<ComplaintEscalation> ComplaintEscalations { get; set; }

    public virtual DbSet<ComplaintHistory> ComplaintHistories { get; set; }

    public virtual DbSet<ComplaintRequest> ComplaintRequests { get; set; }

    public virtual DbSet<ComplaintStatus> ComplaintStatuses { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<EscalationRule> EscalationRules { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<Priority> Priorities { get; set; }

    public virtual DbSet<RequestStatus> RequestStatuses { get; set; }

    public virtual DbSet<RequestType> RequestTypes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleRequest> RoleRequests { get; set; }

    public virtual DbSet<VComplaintDashboard> VComplaintDashboards { get; set; }

    public virtual DbSet<VGroActiveWorkload> VGroActiveWorkloads { get; set; }

    public virtual DbSet<VSlaBreachedComplaint> VSlaBreachedComplaints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("pk_audit_logs");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.ChangedAt, "idx_audit_changed_at");

            entity.HasIndex(e => new { e.TableName, e.RecordId }, "idx_audit_table_record");

            entity.Property(e => e.AuditLogId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("audit_log_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.ColumnName)
                .HasMaxLength(100)
                .HasColumnName("column_name");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .HasColumnName("table_name");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("fk_audit_changed_by");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("pk_categories");

            entity.ToTable("categories");

            entity.HasIndex(e => new { e.CategoryName, e.DepartmentId }, "uq_category_department").IsUnique();

            entity.Property(e => e.CategoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DefaultPriorityId).HasColumnName("default_priority_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SlaHours).HasColumnName("sla_hours");

            entity.HasOne(d => d.DefaultPriority).WithMany(p => p.Categories)
                .HasForeignKey(d => d.DefaultPriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_category_priority");

            entity.HasOne(d => d.Department).WithMany(p => p.Categories)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_category_department");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("pk_complaints");

            entity.ToTable("complaints");

            entity.HasIndex(e => e.CategoryId, "idx_complaint_category");

            entity.HasIndex(e => e.CreatedAt, "idx_complaint_created_at");

            entity.HasIndex(e => e.EscalationDueAt, "idx_complaint_escalation_due").HasFilter("(escalation_due_at IS NOT NULL)");

            entity.HasIndex(e => e.CurrentHandlerEmployeeId, "idx_complaint_owner");

            entity.HasIndex(e => e.PriorityId, "idx_complaint_priority");

            entity.HasIndex(e => e.RaisedByEmployeeId, "idx_complaint_raised_by");

            entity.HasIndex(e => e.StatusId, "idx_complaint_status");

            entity.Property(e => e.ComplaintId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("complaint_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ComplaintDescription).HasColumnName("complaint_description");
            entity.Property(e => e.ComplaintTitle)
                .HasMaxLength(250)
                .HasColumnName("complaint_title");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentHandlerEmployeeId).HasColumnName("current_handler_employee_id");
            entity.Property(e => e.EscalationDueAt).HasColumnName("escalation_due_at");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.RaisedByEmployeeId).HasColumnName("raised_by_employee_id");
            entity.Property(e => e.ReopenedCount).HasColumnName("reopened_count");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_category");

            entity.HasOne(d => d.CurrentHandlerEmployee).WithMany(p => p.ComplaintCurrentHandlerEmployees)
                .HasForeignKey(d => d.CurrentHandlerEmployeeId)
                .HasConstraintName("fk_complaint_owner");

            entity.HasOne(d => d.Priority).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_priority");

            entity.HasOne(d => d.RaisedByEmployee).WithMany(p => p.ComplaintRaisedByEmployees)
                .HasForeignKey(d => d.RaisedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_raised_by");

            entity.HasOne(d => d.Status).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_status");
        });

        modelBuilder.Entity<ComplaintAssignmentHistory>(entity =>
        {
            entity.HasKey(e => e.AssignmentHistoryId).HasName("pk_complaint_assignment_history");

            entity.ToTable("complaint_assignment_history");

            entity.HasIndex(e => e.ComplaintId, "idx_assign_history_complaint");

            entity.HasIndex(e => new { e.ComplaintId, e.NewHandlerEmployeeId }, "uq_complaint_handler_unique").IsUnique();

            entity.Property(e => e.AssignmentHistoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("assignment_history_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.AssignmentReason)
                .HasMaxLength(50)
                .HasDefaultValueSql("'INITIAL'::character varying")
                .HasColumnName("assignment_reason");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.NewHandlerEmployeeId).HasColumnName("new_handler_employee_id");
            entity.Property(e => e.OldHandlerEmployeeId).HasColumnName("old_handler_employee_id");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.ComplaintAssignmentHistoryAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assign_history_assigned_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintAssignmentHistories)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assign_history_complaint");

            entity.HasOne(d => d.NewHandlerEmployee).WithMany(p => p.ComplaintAssignmentHistoryNewHandlerEmployees)
                .HasForeignKey(d => d.NewHandlerEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assign_history_new_handler");

            entity.HasOne(d => d.OldHandlerEmployee).WithMany(p => p.ComplaintAssignmentHistoryOldHandlerEmployees)
                .HasForeignKey(d => d.OldHandlerEmployeeId)
                .HasConstraintName("fk_assign_history_old_handler");
        });

        modelBuilder.Entity<ComplaintAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("pk_complaint_attachments");

            entity.ToTable("complaint_attachments");

            entity.HasIndex(e => e.ComplaintId, "idx_attachment_complaint");

            entity.Property(e => e.AttachmentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("attachment_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.OriginalFileName)
                .HasMaxLength(255)
                .HasColumnName("original_file_name");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintAttachments)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_attachment_complaint");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.ComplaintAttachments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_attachment_uploaded_by");
        });

        modelBuilder.Entity<ComplaintComment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("pk_complaint_comments");

            entity.ToTable("complaint_comments");

            entity.Property(e => e.CommentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("comment_id");
            entity.Property(e => e.CommentText).HasColumnName("comment_text");
            entity.Property(e => e.CommentedBy).HasColumnName("commented_by");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsInternal).HasColumnName("is_internal");

            entity.HasOne(d => d.CommentedByNavigation).WithMany(p => p.ComplaintComments)
                .HasForeignKey(d => d.CommentedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comment_employee");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintComments)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_comment_complaint");
        });

        modelBuilder.Entity<ComplaintEscalation>(entity =>
        {
            entity.HasKey(e => e.EscalationId).HasName("pk_complaint_escalations");

            entity.ToTable("complaint_escalations");

            entity.HasIndex(e => e.ComplaintId, "idx_escalation_complaint");

            entity.Property(e => e.EscalationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("escalation_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.EscalatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("escalated_at");
            entity.Property(e => e.EscalatedByEmployeeId).HasColumnName("escalated_by_employee_id");
            entity.Property(e => e.EscalatedToEmployeeId).HasColumnName("escalated_to_employee_id");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.EscalationRuleId).HasColumnName("escalation_rule_id");
            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintEscalations)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_escalation_complaint");

            entity.HasOne(d => d.EscalatedByEmployee).WithMany(p => p.ComplaintEscalationEscalatedByEmployees)
                .HasForeignKey(d => d.EscalatedByEmployeeId)
                .HasConstraintName("fk_escalated_by_employee");

            entity.HasOne(d => d.EscalatedToEmployee).WithMany(p => p.ComplaintEscalationEscalatedToEmployees)
                .HasForeignKey(d => d.EscalatedToEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_escalated_to_employee");

            entity.HasOne(d => d.EscalationRule).WithMany(p => p.ComplaintEscalations)
                .HasForeignKey(d => d.EscalationRuleId)
                .HasConstraintName("fk_escalation_rule_ref");
        });

        modelBuilder.Entity<ComplaintHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("pk_complaint_history");

            entity.ToTable("complaint_history");

            entity.HasIndex(e => e.ComplaintId, "idx_history_complaint");

            entity.HasIndex(e => new { e.ComplaintId, e.CreatedAt }, "idx_history_created_at");

            entity.Property(e => e.HistoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("history_id");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EscalationLevelSnapshot).HasColumnName("escalation_level_snapshot");
            entity.Property(e => e.NewHandlerEmployeeId).HasColumnName("new_handler_employee_id");
            entity.Property(e => e.NewStatusId).HasColumnName("new_status_id");
            entity.Property(e => e.OldHandlerEmployeeId).HasColumnName("old_handler_employee_id");
            entity.Property(e => e.OldStatusId).HasColumnName("old_status_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.ComplaintHistoryChangedByNavigations)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("fk_history_changed_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintHistories)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_history_complaint");

            entity.HasOne(d => d.NewHandlerEmployee).WithMany(p => p.ComplaintHistoryNewHandlerEmployees)
                .HasForeignKey(d => d.NewHandlerEmployeeId)
                .HasConstraintName("fk_history_new_handler");

            entity.HasOne(d => d.NewStatus).WithMany(p => p.ComplaintHistoryNewStatuses)
                .HasForeignKey(d => d.NewStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_history_new_status");

            entity.HasOne(d => d.OldHandlerEmployee).WithMany(p => p.ComplaintHistoryOldHandlerEmployees)
                .HasForeignKey(d => d.OldHandlerEmployeeId)
                .HasConstraintName("fk_history_old_handler");

            entity.HasOne(d => d.OldStatus).WithMany(p => p.ComplaintHistoryOldStatuses)
                .HasForeignKey(d => d.OldStatusId)
                .HasConstraintName("fk_history_old_status");
        });

        modelBuilder.Entity<ComplaintRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("pk_complaint_requests");

            entity.ToTable("complaint_requests");

            entity.HasIndex(e => e.ComplaintId, "idx_complaint_request_complaint");

            entity.HasIndex(e => new { e.ComplaintId, e.RequestTypeId }, "uq_pending_complaint_request_per_type")
                .IsUnique()
                .HasFilter("(request_status_id = 1)");

            entity.Property(e => e.RequestId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("request_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.RequestStatusId).HasColumnName("request_status_id");
            entity.Property(e => e.RequestTypeId).HasColumnName("request_type_id");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintRequests)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_request_complaint");

            entity.HasOne(d => d.RequestStatus).WithMany(p => p.ComplaintRequests)
                .HasForeignKey(d => d.RequestStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_request_status");

            entity.HasOne(d => d.RequestType).WithMany(p => p.ComplaintRequests)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_request_type");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.ComplaintRequestRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_complaint_request_requested_by");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ComplaintRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("fk_complaint_request_reviewed_by");
        });

        modelBuilder.Entity<ComplaintStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("pk_complaint_statuses");

            entity.ToTable("complaint_statuses");

            entity.HasIndex(e => e.StatusName, "uq_status_name").IsUnique();

            entity.Property(e => e.StatusId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("status_id");
            entity.Property(e => e.IsTerminal).HasColumnName("is_terminal");
            entity.Property(e => e.StatusName)
                .HasMaxLength(30)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("pk_departments");

            entity.ToTable("departments");

            entity.HasIndex(e => e.DepartmentName, "uq_department_name").IsUnique();

            entity.Property(e => e.DepartmentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("department_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentHeadEmployeeId).HasColumnName("department_head_employee_id");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(100)
                .HasColumnName("department_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.HasOne(d => d.DepartmentHeadEmployee).WithMany(p => p.Departments)
                .HasForeignKey(d => d.DepartmentHeadEmployeeId)
                .HasConstraintName("fk_department_head");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("pk_employees");

            entity.ToTable("employees");

            entity.HasIndex(e => e.DepartmentId, "idx_employee_department");

            entity.HasIndex(e => e.RoleId, "idx_employee_role");

            entity.HasIndex(e => e.Email, "uq_employee_email").IsUnique();

            entity.HasIndex(e => e.RoleId, "uq_single_active_admin")
                .IsUnique()
                .HasFilter("((role_id = 4) AND (is_active = true))");

            entity.Property(e => e.EmployeeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("employee_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(150)
                .HasColumnName("employee_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("mobile_number");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("fk_employee_department");


            entity.HasOne(d => d.Role)
            .WithMany(p => p.Employees)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("fk_employee_role");
        });

        modelBuilder.Entity<EscalationRule>(entity =>
        {
            entity.HasKey(e => e.EscalationRuleId).HasName("pk_escalation_rules");

            entity.ToTable("escalation_rules");

            entity.HasIndex(e => new { e.CategoryId, e.PriorityId, e.EscalationLevel }, "uq_escalation_rule").IsUnique();

            entity.Property(e => e.EscalationRuleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("escalation_rule_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.EscalateAfterHours).HasColumnName("escalate_after_hours");
            entity.Property(e => e.EscalateToRoleId).HasColumnName("escalate_to_role_id");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");

            entity.HasOne(d => d.Category).WithMany(p => p.EscalationRules)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_escalation_rule_category");

            entity.HasOne(d => d.EscalateToRole).WithMany(p => p.EscalationRules)
                .HasForeignKey(d => d.EscalateToRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_escalation_rule_role");

            entity.HasOne(d => d.Priority).WithMany(p => p.EscalationRules)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_escalation_rule_priority");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("pk_notifications");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.ReferenceComplaintId, "idx_notification_complaint").HasFilter("(reference_complaint_id IS NOT NULL)");

            entity.HasIndex(e => new { e.EmployeeId, e.IsRead }, "idx_notification_employee");

            entity.Property(e => e.NotificationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.NotificationTypeId).HasColumnName("notification_type_id");
            entity.Property(e => e.ReferenceComplaintId).HasColumnName("reference_complaint_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");

            entity.HasOne(d => d.Employee).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notification_employee");

            entity.HasOne(d => d.NotificationType).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.NotificationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notification_type");

            entity.HasOne(d => d.ReferenceComplaint).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ReferenceComplaintId)
                .HasConstraintName("fk_notification_complaint");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.NotificationTypeId).HasName("pk_notification_types");

            entity.ToTable("notification_types");

            entity.HasIndex(e => e.NotificationTypeName, "uq_notification_type_name").IsUnique();

            entity.Property(e => e.NotificationTypeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("notification_type_id");
            entity.Property(e => e.NotificationTypeName)
                .HasMaxLength(50)
                .HasColumnName("notification_type_name");
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.HasKey(e => e.PriorityId).HasName("pk_priorities");

            entity.ToTable("priorities");

            entity.HasIndex(e => e.PriorityName, "uq_priority_name").IsUnique();

            entity.HasIndex(e => e.EscalationOrder, "uq_priority_order").IsUnique();

            entity.Property(e => e.PriorityId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("priority_id");
            entity.Property(e => e.EscalationOrder).HasColumnName("escalation_order");
            entity.Property(e => e.PriorityName)
                .HasMaxLength(20)
                .HasColumnName("priority_name");
        });

        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.HasKey(e => e.RequestStatusId).HasName("pk_request_statuses");

            entity.ToTable("request_statuses");

            entity.HasIndex(e => e.StatusName, "uq_request_status_name").IsUnique();

            entity.Property(e => e.RequestStatusId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("request_status_id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(30)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.HasKey(e => e.RequestTypeId).HasName("pk_request_types");

            entity.ToTable("request_types");

            entity.HasIndex(e => e.RequestTypeName, "uq_request_type_name").IsUnique();

            entity.Property(e => e.RequestTypeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("request_type_id");
            entity.Property(e => e.RequestTypeName)
                .HasMaxLength(50)
                .HasColumnName("request_type_name");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("pk_roles");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "uq_role_name").IsUnique();

            entity.Property(e => e.RoleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<RoleRequest>(entity =>
        {
            entity.HasKey(e => e.RoleRequestId).HasName("pk_role_requests");

            entity.ToTable("role_requests");

            entity.HasIndex(e => e.EmployeeId, "idx_role_request_employee");

            entity.HasIndex(e => e.EmployeeId, "uq_pending_role_request")
                .IsUnique()
                .HasFilter("(request_status_id = 1)");

            entity.Property(e => e.RoleRequestId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("role_request_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentRoleId).HasColumnName("current_role_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.RequestStatusId).HasColumnName("request_status_id");
            entity.Property(e => e.RequestedRoleId).HasColumnName("requested_role_id");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");

            entity.HasOne(d => d.CurrentRole).WithMany(p => p.RoleRequestCurrentRoles)
                .HasForeignKey(d => d.CurrentRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_request_current_role");

            entity.HasOne(d => d.Employee)
      .WithMany(p => p.RoleRequests)
      .HasForeignKey(d => d.EmployeeId)
      .OnDelete(DeleteBehavior.ClientSetNull)
      .HasConstraintName("fk_role_request_employee");

            entity.HasOne(d => d.RequestStatus).WithMany(p => p.RoleRequests)
                .HasForeignKey(d => d.RequestStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_request_status");

            entity.HasOne(d => d.RequestedRole).WithMany(p => p.RoleRequestRequestedRoles)
                .HasForeignKey(d => d.RequestedRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_request_requested_role");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RoleRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("fk_role_request_reviewed_by");
        });

        modelBuilder.Entity<VComplaintDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_complaint_dashboard");

            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.ComplaintTitle)
                .HasMaxLength(250)
                .HasColumnName("complaint_title");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CurrentHandlerName)
                .HasMaxLength(150)
                .HasColumnName("current_handler_name");
            entity.Property(e => e.CurrentHandlerRole)
                .HasMaxLength(50)
                .HasColumnName("current_handler_role");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(100)
                .HasColumnName("department_name");
            entity.Property(e => e.EscalationDueAt).HasColumnName("escalation_due_at");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.IsTerminal).HasColumnName("is_terminal");
            entity.Property(e => e.PriorityName)
                .HasMaxLength(20)
                .HasColumnName("priority_name");
            entity.Property(e => e.PriorityWeight).HasColumnName("priority_weight");
            entity.Property(e => e.RaisedByName)
                .HasMaxLength(150)
                .HasColumnName("raised_by_name");
            entity.Property(e => e.ReopenedCount).HasColumnName("reopened_count");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.StatusName)
                .HasMaxLength(30)
                .HasColumnName("status_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<VGroActiveWorkload>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_gro_active_workload");

            entity.Property(e => e.ActiveComplaintCount).HasColumnName("active_complaint_count");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.EmployeeCreatedAt).HasColumnName("employee_created_at");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(150)
                .HasColumnName("employee_name");
            entity.Property(e => e.WeightedScore).HasColumnName("weighted_score");
        });

        modelBuilder.Entity<VSlaBreachedComplaint>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_sla_breached_complaints");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.ComplaintTitle)
                .HasMaxLength(250)
                .HasColumnName("complaint_title");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CurrentHandlerEmployeeId).HasColumnName("current_handler_employee_id");
            entity.Property(e => e.EscalationDueAt).HasColumnName("escalation_due_at");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
