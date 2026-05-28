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

    public virtual DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }

    public virtual DbSet<ComplaintComment> ComplaintComments { get; set; }

    public virtual DbSet<ComplaintOfficer> ComplaintOfficers { get; set; }

    public virtual DbSet<ComplaintStatus> ComplaintStatuses { get; set; }

    public virtual DbSet<ComplaintStatusHistory> ComplaintStatusHistories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Escalation> Escalations { get; set; }

    public virtual DbSet<EscalationRule> EscalationRules { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Priority> Priorities { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleRequest> RoleRequests { get; set; }

    public virtual DbSet<VwComplaintSummary> VwComplaintSummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("pk_audit_logs");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.ChangedAt, "ix_audit_logs_changed_at").IsDescending();

            entity.HasIndex(e => e.ChangedByEmployeeId, "ix_audit_logs_changed_by").HasFilter("(changed_by_employee_id IS NOT NULL)");

            entity.HasIndex(e => new { e.TableName, e.RecordId }, "ix_audit_logs_table_record");

            entity.Property(e => e.AuditId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("audit_id");
            entity.Property(e => e.ApplicationContext)
                .HasMaxLength(200)
                .HasColumnName("application_context");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedByEmployeeId).HasColumnName("changed_by_employee_id");
            entity.Property(e => e.ColumnName)
                .HasMaxLength(100)
                .HasColumnName("column_name");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.Operation)
                .HasMaxLength(1)
                .HasColumnName("operation");
            entity.Property(e => e.RecordId)
                .HasMaxLength(100)
                .HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .HasColumnName("table_name");

            entity.HasOne(d => d.ChangedByEmployee).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ChangedByEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_audit_changed_by");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("pk_categories");

            entity.ToTable("categories");

            entity.HasIndex(e => e.ParentCategoryId, "ix_categories_parent").HasFilter("(parent_category_id IS NOT NULL)");

            entity.HasIndex(e => e.CategoryName, "uq_categories_name").IsUnique();

            entity.Property(e => e.CategoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(150)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DefaultSlaHours)
                .HasDefaultValue((short)48)
                .HasColumnName("default_sla_hours");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_categories_parent");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("pk_complaints");

            entity.ToTable("complaints");

            entity.HasIndex(e => new { e.CategoryId, e.PriorityId }, "ix_complaints_category_priority");

            entity.HasIndex(e => e.CreatedAt, "ix_complaints_created_at").IsDescending();

            entity.HasIndex(e => new { e.DepartmentId, e.StatusId }, "ix_complaints_department_status");

            entity.HasIndex(e => e.RaisedByEmployeeId, "ix_complaints_raised_by");

            entity.HasIndex(e => e.SlaDeadline, "ix_complaints_sla_deadline").HasFilter("(sla_breached = false)");

            entity.HasIndex(e => e.StatusId, "ix_complaints_status");

            entity.HasIndex(e => e.ComplaintNumber, "uq_complaints_number").IsUnique();

            entity.Property(e => e.ComplaintId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("complaint_id");
            entity.Property(e => e.AssignedToEmployeeId).HasColumnName("assigned_to_employee_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ComplaintNumber)
                .HasMaxLength(20)
                .HasDefaultValueSql("((('CGR-'::text || to_char(now(), 'YYYY'::text)) || '-'::text) || lpad((nextval('seq_complaint_number'::regclass))::text, 6, '0'::text))")
                .HasColumnName("complaint_number");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImpactDescription).HasColumnName("impact_description");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.RaisedByEmployeeId).HasColumnName("raised_by_employee_id");
            entity.Property(e => e.ResolutionRemarks).HasColumnName("resolution_remarks");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.SlaBreached).HasColumnName("sla_breached");
            entity.Property(e => e.SlaDeadline).HasColumnName("sla_deadline");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Title)
                .HasMaxLength(300)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.AssignedToEmployee).WithMany(p => p.ComplaintAssignedToEmployees)
                .HasForeignKey(d => d.AssignedToEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_complaints_assigned_to");

            entity.HasOne(d => d.Category).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_complaints_category");

            entity.HasOne(d => d.Department).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_complaints_department");

            entity.HasOne(d => d.Priority).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_complaints_priority");

            entity.HasOne(d => d.RaisedByEmployee).WithMany(p => p.ComplaintRaisedByEmployees)
                .HasForeignKey(d => d.RaisedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_complaints_raised_by");

            entity.HasOne(d => d.Status).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_complaints_status");
        });

        modelBuilder.Entity<ComplaintAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("pk_complaint_attachments");

            entity.ToTable("complaint_attachments");

            entity.HasIndex(e => e.ComplaintId, "ix_attachments_complaint").HasFilter("(is_deleted = false)");

            entity.Property(e => e.AttachmentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("attachment_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.MimeType)
                .HasMaxLength(127)
                .HasColumnName("mime_type");
            entity.Property(e => e.StoragePath)
                .HasMaxLength(1000)
                .HasColumnName("storage_path");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintAttachments)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_attachments_complaint");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.ComplaintAttachments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_attachments_uploader");
        });

        modelBuilder.Entity<ComplaintComment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("pk_complaint_comments");

            entity.ToTable("complaint_comments");

            entity.HasIndex(e => new { e.ComplaintId, e.CreatedAt }, "ix_comments_complaint").HasFilter("(is_deleted = false)");

            entity.Property(e => e.CommentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("comment_id");
            entity.Property(e => e.CommentText).HasColumnName("comment_text");
            entity.Property(e => e.CommentedBy).HasColumnName("commented_by");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.IsInternal).HasColumnName("is_internal");
            entity.Property(e => e.ParentCommentId).HasColumnName("parent_comment_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CommentedByNavigation).WithMany(p => p.ComplaintComments)
                .HasForeignKey(d => d.CommentedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_comments_author");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintComments)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_comments_complaint");

            entity.HasOne(d => d.ParentComment).WithMany(p => p.InverseParentComment)
                .HasForeignKey(d => d.ParentCommentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_comments_parent");
        });

        modelBuilder.Entity<ComplaintOfficer>(entity =>
        {
            entity.HasKey(e => e.CoId).HasName("pk_complaint_officers");

            entity.ToTable("complaint_officers");

            entity.HasIndex(e => e.EmployeeId, "ix_complaint_officers_employee").HasFilter("(is_active = true)");

            entity.HasIndex(e => e.ComplaintId, "uix_complaint_officers_one_primary")
                .IsUnique()
                .HasFilter("(((role_in_complaint)::text = 'PRIMARY'::text) AND (is_active = true))");

            entity.HasIndex(e => new { e.ComplaintId, e.EmployeeId }, "uq_complaint_officers_employee").IsUnique();

            entity.Property(e => e.CoId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("co_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RoleInComplaint)
                .HasMaxLength(20)
                .HasDefaultValueSql("'COLLABORATOR'::character varying")
                .HasColumnName("role_in_complaint");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.ComplaintOfficerAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_co_assigned_by");

            entity.HasOne(d => d.Complaint).WithOne(p => p.ComplaintOfficer)
                .HasForeignKey<ComplaintOfficer>(d => d.ComplaintId)
                .HasConstraintName("fk_co_complaint");

            entity.HasOne(d => d.Employee).WithMany(p => p.ComplaintOfficerEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_co_employee");
        });

        modelBuilder.Entity<ComplaintStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("pk_complaint_statuses");

            entity.ToTable("complaint_statuses");

            entity.HasIndex(e => e.StatusName, "uq_complaint_statuses_name").IsUnique();

            entity.Property(e => e.StatusId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("status_id");
            entity.Property(e => e.DisplayOrder).HasColumnName("display_order");
            entity.Property(e => e.IsTerminal).HasColumnName("is_terminal");
            entity.Property(e => e.StatusName)
                .HasMaxLength(80)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<ComplaintStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("pk_complaint_status_history");

            entity.ToTable("complaint_status_history");

            entity.HasIndex(e => new { e.ComplaintId, e.ChangedAt }, "ix_status_history_complaint").IsDescending(false, true);

            entity.Property(e => e.HistoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("history_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedByEmployeeId).HasColumnName("changed_by_employee_id");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.FromStatusId).HasColumnName("from_status_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.ToStatusId).HasColumnName("to_status_id");

            entity.HasOne(d => d.ChangedByEmployee).WithMany(p => p.ComplaintStatusHistories)
                .HasForeignKey(d => d.ChangedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_csh_changed_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintStatusHistories)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_csh_complaint");

            entity.HasOne(d => d.FromStatus).WithMany(p => p.ComplaintStatusHistoryFromStatuses)
                .HasForeignKey(d => d.FromStatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_csh_from_status");

            entity.HasOne(d => d.ToStatus).WithMany(p => p.ComplaintStatusHistoryToStatuses)
                .HasForeignKey(d => d.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_csh_to_status");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("pk_departments");

            entity.ToTable("departments");

            entity.HasIndex(e => e.ParentDeptId, "ix_departments_parent").HasFilter("(parent_dept_id IS NOT NULL)");

            entity.HasIndex(e => e.DepartmentName, "uq_departments_name").IsUnique();

            entity.Property(e => e.DepartmentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("department_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(150)
                .HasColumnName("department_name");
            entity.Property(e => e.HeadEmployeeId).HasColumnName("head_employee_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ParentDeptId).HasColumnName("parent_dept_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.HeadEmployee).WithMany(p => p.Departments)
                .HasForeignKey(d => d.HeadEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_departments_head");

            entity.HasOne(d => d.ParentDept).WithMany(p => p.InverseParentDept)
                .HasForeignKey(d => d.ParentDeptId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_departments_parent");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("pk_employees");

            entity.ToTable("employees");

            entity.HasIndex(e => e.DepartmentId, "ix_employees_department");

            entity.HasIndex(e => e.RoleId, "ix_employees_role");

            entity.HasIndex(e => e.Email, "uq_employees_email").IsUnique();

            entity.Property(e => e.EmployeeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("employee_id");
            entity.Property(e => e.AddressEncrypted).HasColumnName("address_encrypted");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MobileNumber).HasColumnName("mobile_number");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_employees_department");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_employees_role");
        });

        modelBuilder.Entity<Escalation>(entity =>
        {
            entity.HasKey(e => e.EscalationId).HasName("pk_escalations");

            entity.ToTable("escalations");

            entity.HasIndex(e => e.ComplaintId, "ix_escalations_complaint");

            entity.HasIndex(e => e.ComplaintId, "ix_escalations_unresolved").HasFilter("(resolved_at IS NULL)");

            entity.HasIndex(e => new { e.ComplaintId, e.EscalationLevel }, "uq_escalations_complaint_level").IsUnique();

            entity.Property(e => e.EscalationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("escalation_id");
            entity.Property(e => e.AcknowledgedAt).HasColumnName("acknowledged_at");
            entity.Property(e => e.AcknowledgedBy).HasColumnName("acknowledged_by");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.EscalatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("escalated_at");
            entity.Property(e => e.EscalatedToEmployeeId).HasColumnName("escalated_to_employee_id");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.RuleId).HasColumnName("rule_id");

            entity.HasOne(d => d.AcknowledgedByNavigation).WithMany(p => p.EscalationAcknowledgedByNavigations)
                .HasForeignKey(d => d.AcknowledgedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_escalations_ack_by");

            entity.HasOne(d => d.Complaint).WithMany(p => p.Escalations)
                .HasForeignKey(d => d.ComplaintId)
                .HasConstraintName("fk_escalations_complaint");

            entity.HasOne(d => d.EscalatedToEmployee).WithMany(p => p.EscalationEscalatedToEmployees)
                .HasForeignKey(d => d.EscalatedToEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_escalations_to_employee");

            entity.HasOne(d => d.Rule).WithMany(p => p.Escalations)
                .HasForeignKey(d => d.RuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_escalations_rule");
        });

        modelBuilder.Entity<EscalationRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("pk_escalation_rules");

            entity.ToTable("escalation_rules");

            entity.Property(e => e.RuleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("rule_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EscalateToRoleId).HasColumnName("escalate_to_role_id");
            entity.Property(e => e.EscalationLevel).HasColumnName("escalation_level");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.NotifyByEmail)
                .HasDefaultValue(true)
                .HasColumnName("notify_by_email");
            entity.Property(e => e.NotifyByInapp)
                .HasDefaultValue(true)
                .HasColumnName("notify_by_inapp");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.RuleName)
                .HasMaxLength(150)
                .HasColumnName("rule_name");
            entity.Property(e => e.TriggerAfterHours).HasColumnName("trigger_after_hours");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.EscalationRules)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_escalation_category");

            entity.HasOne(d => d.EscalateToRole).WithMany(p => p.EscalationRules)
                .HasForeignKey(d => d.EscalateToRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_escalation_role");

            entity.HasOne(d => d.Priority).WithMany(p => p.EscalationRules)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_escalation_priority");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("pk_notifications");

            entity.ToTable("notifications");

            entity.HasIndex(e => new { e.RecipientEmployeeId, e.IsSent }, "ix_notifications_recipient_unsent").HasFilter("(is_sent = false)");

            entity.Property(e => e.NotificationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("notification_id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.Channel)
                .HasMaxLength(10)
                .HasDefaultValueSql("'IN_APP'::character varying")
                .HasColumnName("channel");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsSent).HasColumnName("is_sent");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.RecipientEmployeeId).HasColumnName("recipient_employee_id");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.Subject)
                .HasMaxLength(300)
                .HasColumnName("subject");

            entity.HasOne(d => d.Complaint).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ComplaintId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_notifications_complaint");

            entity.HasOne(d => d.RecipientEmployee).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RecipientEmployeeId)
                .HasConstraintName("fk_notifications_recipient");
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.HasKey(e => e.PriorityId).HasName("pk_priorities");

            entity.ToTable("priorities");

            entity.HasIndex(e => e.PriorityName, "uq_priorities_name").IsUnique();

            entity.Property(e => e.PriorityId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("priority_id");
            entity.Property(e => e.EscalationHours)
                .HasDefaultValue((short)24)
                .HasColumnName("escalation_hours");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PriorityName)
                .HasMaxLength(50)
                .HasColumnName("priority_name");
            entity.Property(e => e.SlaMultiplier)
                .HasPrecision(4, 2)
                .HasDefaultValue(1.00m)
                .HasColumnName("sla_multiplier");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("pk_roles");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "uq_roles_name").IsUnique();

            entity.Property(e => e.RoleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<RoleRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("pk_role_requests");

            entity.ToTable("role_requests");

            entity.HasIndex(e => e.RequesterEmail, "ix_role_requests_email");

            entity.HasIndex(e => e.Status, "ix_role_requests_status").HasFilter("((status)::text = 'PENDING'::text)");

            entity.HasIndex(e => new { e.RequesterEmail, e.RequestedRoleId }, "uix_role_requests_pending")
                .IsUnique()
                .HasFilter("((status)::text = 'PENDING'::text)");

            entity.Property(e => e.RequestId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("request_id");
            entity.Property(e => e.ApprovedEmployeeId).HasColumnName("approved_employee_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Justification).HasColumnName("justification");
            entity.Property(e => e.RequestedDeptId).HasColumnName("requested_dept_id");
            entity.Property(e => e.RequestedRoleId).HasColumnName("requested_role_id");
            entity.Property(e => e.RequesterAddressEncrypted).HasColumnName("requester_address_encrypted");
            entity.Property(e => e.RequesterEmail)
                .HasMaxLength(254)
                .HasColumnName("requester_email");
            entity.Property(e => e.RequesterMobile).HasColumnName("requester_mobile");
            entity.Property(e => e.RequesterName)
                .HasMaxLength(200)
                .HasColumnName("requester_name");
            entity.Property(e => e.ReviewRemarks).HasColumnName("review_remarks");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ApprovedEmployee).WithMany(p => p.RoleRequestApprovedEmployees)
                .HasForeignKey(d => d.ApprovedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_role_requests_approved_employee");

            entity.HasOne(d => d.RequestedDept).WithMany(p => p.RoleRequests)
                .HasForeignKey(d => d.RequestedDeptId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_role_requests_department");

            entity.HasOne(d => d.RequestedRole).WithMany(p => p.RoleRequests)
                .HasForeignKey(d => d.RequestedRoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_role_requests_role");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RoleRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_role_requests_reviewer");
        });

        modelBuilder.Entity<VwComplaintSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_complaint_summary");

            entity.Property(e => e.AssignedAt).HasColumnName("assigned_at");
            entity.Property(e => e.AssignedToName)
                .HasMaxLength(200)
                .HasColumnName("assigned_to_name");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(150)
                .HasColumnName("category_name");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ComplaintId).HasColumnName("complaint_id");
            entity.Property(e => e.ComplaintNumber)
                .HasMaxLength(20)
                .HasColumnName("complaint_number");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(150)
                .HasColumnName("department_name");
            entity.Property(e => e.IsTerminal).HasColumnName("is_terminal");
            entity.Property(e => e.PriorityName)
                .HasMaxLength(50)
                .HasColumnName("priority_name");
            entity.Property(e => e.RaisedByEmail)
                .HasMaxLength(254)
                .HasColumnName("raised_by_email");
            entity.Property(e => e.RaisedByName)
                .HasMaxLength(200)
                .HasColumnName("raised_by_name");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.SlaBreached).HasColumnName("sla_breached");
            entity.Property(e => e.SlaDeadline).HasColumnName("sla_deadline");
            entity.Property(e => e.SlaHoursRemaining).HasColumnName("sla_hours_remaining");
            entity.Property(e => e.SlaMultiplier)
                .HasPrecision(4, 2)
                .HasColumnName("sla_multiplier");
            entity.Property(e => e.StatusName)
                .HasMaxLength(80)
                .HasColumnName("status_name");
            entity.Property(e => e.Title)
                .HasMaxLength(300)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
        modelBuilder.HasSequence("seq_complaint_number");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
