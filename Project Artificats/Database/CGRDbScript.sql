-- 1 Roles

CREATE TABLE IF NOT EXISTS roles (
    role_id     SMALLINT        GENERATED ALWAYS AS IDENTITY,
    role_name   VARCHAR(50)     NOT NULL,
    description TEXT            NULL,
    is_active   BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_roles
        PRIMARY KEY (role_id),

    CONSTRAINT uq_roles_name
        UNIQUE (role_name)
);

-- 2 Categories
CREATE TABLE IF NOT EXISTS categories (
    category_id         INTEGER         GENERATED ALWAYS AS IDENTITY,
    category_name       VARCHAR(150)    NOT NULL,
    parent_category_id  INTEGER         NULL,
    default_sla_hours   SMALLINT        NOT NULL DEFAULT 48,
    is_active           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_categories
        PRIMARY KEY (category_id),

    CONSTRAINT uq_categories_name
        UNIQUE (category_name),

    CONSTRAINT fk_categories_parent
        FOREIGN KEY (parent_category_id)
        REFERENCES categories(category_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT ck_categories_sla_positive
        CHECK (default_sla_hours > 0),

    CONSTRAINT ck_categories_name_nonempty
        CHECK (TRIM(category_name) <> '')
);

CREATE INDEX ix_categories_parent
    ON categories(parent_category_id)
    WHERE parent_category_id IS NOT NULL;

-- 3 Priorities

CREATE TABLE IF NOT EXISTS priorities (
    priority_id         SMALLINT        GENERATED ALWAYS AS IDENTITY,
    priority_name       VARCHAR(50)     NOT NULL,
    sla_multiplier      NUMERIC(4,2)    NOT NULL DEFAULT 1.00,
    escalation_hours    SMALLINT        NOT NULL DEFAULT 24,
    is_active           BOOLEAN         NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_priorities
        PRIMARY KEY (priority_id),

    CONSTRAINT uq_priorities_name
        UNIQUE (priority_name),

    CONSTRAINT ck_priorities_multiplier
        CHECK (sla_multiplier > 0),

    CONSTRAINT ck_priorities_esc_hours
        CHECK (escalation_hours > 0)
);

-- 4 Compliant Statuses

CREATE TABLE IF NOT EXISTS complaint_statuses (
    status_id       SMALLINT        GENERATED ALWAYS AS IDENTITY,
    status_name     VARCHAR(80)     NOT NULL,
    is_terminal     BOOLEAN         NOT NULL DEFAULT FALSE,
    display_order   SMALLINT        NOT NULL DEFAULT 0,

    CONSTRAINT pk_complaint_statuses
        PRIMARY KEY (status_id),

    CONSTRAINT uq_complaint_statuses_name
        UNIQUE (status_name)
);

-- 5. Departments

CREATE TABLE IF NOT EXISTS departments (
    department_id       INTEGER         GENERATED ALWAYS AS IDENTITY,
    department_name     VARCHAR(150)    NOT NULL,
    parent_dept_id      INTEGER         NULL,
    head_employee_id    INTEGER         NULL,
    is_active           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_departments
        PRIMARY KEY (department_id),

    CONSTRAINT uq_departments_name
        UNIQUE (department_name),

    CONSTRAINT fk_departments_parent
        FOREIGN KEY (parent_dept_id)
        REFERENCES departments(department_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,


    CONSTRAINT ck_departments_name_nonempty
        CHECK (TRIM(department_name) <> '')
);

CREATE INDEX ix_departments_parent
    ON departments(parent_dept_id)
    WHERE parent_dept_id IS NOT NULL;




--- 6 Employees

CREATE TABLE IF NOT EXISTS employees (
    employee_id          INTEGER         GENERATED ALWAYS AS IDENTITY,
    full_name            VARCHAR(200)    NOT NULL,
    email                VARCHAR(254)    NOT NULL,
    mobile_number        TEXT            NULL,
    address_encrypted    TEXT            NULL,
    password_hash        TEXT    NOT NULL,
    department_id        INTEGER         NOT NULL,
    role_id              SMALLINT        NOT NULL,
    is_active            BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_employees
        PRIMARY KEY (employee_id),

    CONSTRAINT uq_employees_email
        UNIQUE (email),

    CONSTRAINT fk_employees_department
        FOREIGN KEY (department_id)
        REFERENCES departments(department_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT fk_employees_role
        FOREIGN KEY (role_id)
        REFERENCES roles(role_id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT ck_employees_email_fmt
        CHECK (email ~ '^[^@\s]+@[^@\s]+\.[^@\s]+$'),

    CONSTRAINT ck_employees_name_nonempty
        CHECK (TRIM(full_name) <> '')
);


CREATE INDEX ix_employees_department
    ON employees(department_id);

CREATE INDEX ix_employees_role
    ON employees(role_id);
-- circular fk
ALTER TABLE departments 
ADD CONSTRAINT fk_departments_head 
FOREIGN KEY (head_employee_id) 
REFERENCES employees(employee_id)
ON DELETE SET NULL 
ON UPDATE CASCADE 
DEFERRABLE INITIALLY DEFERRED;
-- 7 Role Requests

CREATE TABLE IF NOT EXISTS role_requests (
    request_id                   INTEGER         GENERATED ALWAYS AS IDENTITY,
    requester_name               VARCHAR(200)    NOT NULL,
    requester_email              VARCHAR(254)    NOT NULL,
    requester_mobile             TEXT            NULL,
    requester_address_encrypted  TEXT            NULL,
    requested_role_id            SMALLINT        NOT NULL,
    requested_dept_id            INTEGER         NOT NULL,
    justification                TEXT            NULL,
    status                       VARCHAR(10)     NOT NULL DEFAULT 'PENDING',
    reviewed_by                  INTEGER         NULL,
    reviewed_at                  TIMESTAMPTZ     NULL,
    review_remarks               TEXT            NULL,
    approved_employee_id         INTEGER         NULL,
    created_at                   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at                   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_role_requests
        PRIMARY KEY (request_id),


    CONSTRAINT fk_role_requests_role
        FOREIGN KEY (requested_role_id)
        REFERENCES roles(role_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_role_requests_department
        FOREIGN KEY (requested_dept_id)
        REFERENCES departments(department_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_role_requests_reviewer
        FOREIGN KEY (reviewed_by)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_role_requests_approved_employee
        FOREIGN KEY (approved_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT ck_role_requests_status
        CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),

    CONSTRAINT ck_role_requests_review_consistency
        CHECK (
            (status = 'PENDING'  AND reviewed_by IS NULL  AND reviewed_at IS NULL)
         OR (status = 'APPROVED' AND reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)
         OR (status = 'REJECTED' AND reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)
        ),

    CONSTRAINT ck_role_requests_approved_emp_only_on_approval
        CHECK (
            approved_employee_id IS NULL
            OR status = 'APPROVED'
        ),

    CONSTRAINT ck_role_requests_email_fmt
        CHECK (requester_email ~ '^[^@\s]+@[^@\s]+\.[^@\s]+$'),

    CONSTRAINT ck_role_requests_name_nonempty
        CHECK (TRIM(requester_name) <> '')
);

CREATE UNIQUE INDEX uix_role_requests_pending
ON role_requests(requester_email, requested_role_id)
WHERE status = 'PENDING';
CREATE INDEX ix_role_requests_status
    ON role_requests(status)
    WHERE status = 'PENDING';

CREATE INDEX ix_role_requests_email
    ON role_requests(requester_email);




-- 8 Complaints table

-- Compliant sequence constructor
CREATE SEQUENCE seq_complaint_number
    START WITH 1
    INCREMENT BY 1
    NO CYCLE;

-- table
CREATE TABLE IF NOT EXISTS complaints (
    complaint_id            BIGINT          GENERATED ALWAYS AS IDENTITY,
    complaint_number        VARCHAR(20)     NOT NULL
                                DEFAULT ('CGR-' || TO_CHAR(NOW(), 'YYYY') || '-'
                                         || LPAD(nextval('seq_complaint_number')::TEXT, 6, '0')),
    title                   VARCHAR(300)    NOT NULL,
    description             TEXT            NOT NULL,
    impact_description      TEXT            NULL,
    category_id             INTEGER         NOT NULL,
    priority_id             SMALLINT        NULL,
    status_id               SMALLINT        NOT NULL,
    raised_by_employee_id   INTEGER         NOT NULL,
	assigned_to_employee_id INTEGER 		NULL,
    department_id           INTEGER         NOT NULL,
    sla_deadline            TIMESTAMPTZ     NULL,
    sla_breached            BOOLEAN         NOT NULL DEFAULT FALSE,
    resolved_at             TIMESTAMPTZ     NULL,
    closed_at               TIMESTAMPTZ     NULL,
    resolution_remarks      TEXT            NULL,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_complaints
        PRIMARY KEY (complaint_id),

    CONSTRAINT uq_complaints_number
        UNIQUE (complaint_number),

    CONSTRAINT fk_complaints_category
        FOREIGN KEY (category_id)
        REFERENCES categories(category_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_complaints_priority
        FOREIGN KEY (priority_id)
        REFERENCES priorities(priority_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_complaints_status
        FOREIGN KEY (status_id)
        REFERENCES complaint_statuses(status_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_complaints_raised_by
        FOREIGN KEY (raised_by_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,
	CONSTRAINT fk_complaints_assigned_to
    FOREIGN KEY (assigned_to_employee_id)
    REFERENCES employees(employee_id)
    ON DELETE SET NULL
    ON UPDATE CASCADE,
    CONSTRAINT fk_complaints_department
        FOREIGN KEY (department_id)
        REFERENCES departments(department_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT ck_complaints_sla_deadline_with_priority
        CHECK (
            (priority_id IS NULL AND sla_deadline IS NULL)
            OR (priority_id IS NOT NULL AND sla_deadline IS NOT NULL)
        ),

    CONSTRAINT ck_complaints_resolved_after_created
        CHECK (resolved_at IS NULL OR resolved_at >= created_at),

    CONSTRAINT ck_complaints_closed_after_resolved
        CHECK (closed_at IS NULL OR resolved_at IS NOT NULL),

    CONSTRAINT ck_complaints_title_nonempty
        CHECK (TRIM(title) <> ''),

    CONSTRAINT ck_complaints_description_nonempty
        CHECK (TRIM(description) <> '')
);

CREATE INDEX ix_complaints_raised_by
    ON complaints(raised_by_employee_id);

CREATE INDEX ix_complaints_status
    ON complaints(status_id);

CREATE INDEX ix_complaints_sla_deadline
    ON complaints(sla_deadline)
    WHERE sla_breached = FALSE;

CREATE INDEX ix_complaints_department_status
    ON complaints(department_id, status_id);

CREATE INDEX ix_complaints_category_priority
    ON complaints(category_id, priority_id);

CREATE INDEX ix_complaints_created_at
    ON complaints(created_at DESC);

-- 9 Compliant officers
CREATE TABLE IF NOT EXISTS complaint_officers (
    co_id                   BIGINT          GENERATED ALWAYS AS IDENTITY,
    complaint_id            BIGINT          NOT NULL,
    employee_id             INTEGER         NOT NULL,
    role_in_complaint       VARCHAR(20)     NOT NULL DEFAULT 'COLLABORATOR',
    assigned_at             TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    assigned_by             INTEGER         NOT NULL,
    is_active               BOOLEAN         NOT NULL DEFAULT TRUE,

    CONSTRAINT pk_complaint_officers
        PRIMARY KEY (co_id),

    CONSTRAINT uq_complaint_officers_employee
        UNIQUE (complaint_id, employee_id),

    CONSTRAINT fk_co_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON DELETE CASCADE ON UPDATE CASCADE,

    CONSTRAINT fk_co_employee
        FOREIGN KEY (employee_id)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_co_assigned_by
        FOREIGN KEY (assigned_by)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT ck_co_role_in_complaint
        CHECK (role_in_complaint IN ('PRIMARY', 'COLLABORATOR')),

    CONSTRAINT ck_co_no_self_assign
        CHECK (employee_id <> assigned_by
               OR role_in_complaint = 'PRIMARY')
);
CREATE UNIQUE INDEX uix_complaint_officers_one_primary
    ON complaint_officers(complaint_id)
    WHERE role_in_complaint = 'PRIMARY' AND is_active = TRUE;


CREATE INDEX ix_complaint_officers_employee
    ON complaint_officers(employee_id)
    WHERE is_active = TRUE;

-- 10 Complaints attachment

CREATE TABLE IF NOT EXISTS complaint_attachments (
    attachment_id   BIGINT          GENERATED ALWAYS AS IDENTITY,
    complaint_id    BIGINT          NOT NULL,
    uploaded_by     INTEGER         NOT NULL,
    file_name       VARCHAR(255)    NOT NULL,
    mime_type       VARCHAR(127)    NOT NULL,
    file_size_bytes BIGINT          NOT NULL,
    storage_path    VARCHAR(1000)   NOT NULL,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_complaint_attachments
        PRIMARY KEY (attachment_id),

    CONSTRAINT fk_attachments_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_attachments_uploader
        FOREIGN KEY (uploaded_by)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT ck_attachments_file_size
        CHECK (file_size_bytes > 0),

    CONSTRAINT ck_attachments_filename_nonempty
        CHECK (TRIM(file_name) <> ''),

    CONSTRAINT ck_attachments_path_nonempty
        CHECK (TRIM(storage_path) <> ''),

    CONSTRAINT ck_attachments_mime_nonempty
        CHECK (TRIM(mime_type) <> '')
);


CREATE INDEX ix_attachments_complaint
    ON complaint_attachments(complaint_id)
    WHERE is_deleted = FALSE;

-- 11 Complaint Comments
CREATE TABLE IF NOT EXISTS complaint_comments (
    comment_id          BIGINT          GENERATED ALWAYS AS IDENTITY,
    complaint_id        BIGINT          NOT NULL,
    commented_by        INTEGER         NOT NULL,
    comment_text        TEXT            NOT NULL,
    is_internal         BOOLEAN         NOT NULL DEFAULT FALSE,
    parent_comment_id   BIGINT          NULL,
    is_deleted          BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_complaint_comments
        PRIMARY KEY (comment_id),

    CONSTRAINT fk_comments_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_comments_author
        FOREIGN KEY (commented_by)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_comments_parent
        FOREIGN KEY (parent_comment_id)
        REFERENCES complaint_comments(comment_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

   

    CONSTRAINT ck_comments_text_nonempty
        CHECK (TRIM(comment_text) <> '')
);


CREATE INDEX ix_comments_complaint
    ON complaint_comments(complaint_id, created_at)
    WHERE is_deleted = FALSE;

-- 12 Compliant Status History

CREATE TABLE IF NOT EXISTS complaint_status_history (
    history_id              BIGINT          GENERATED ALWAYS AS IDENTITY,
    complaint_id            BIGINT          NOT NULL,
    from_status_id          SMALLINT        NULL,
    to_status_id            SMALLINT        NOT NULL,
    changed_by_employee_id  INTEGER         NOT NULL,
    remarks                 TEXT            NULL,
    changed_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_complaint_status_history
        PRIMARY KEY (history_id),

    CONSTRAINT fk_csh_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_csh_from_status
        FOREIGN KEY (from_status_id)
        REFERENCES complaint_statuses(status_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_csh_to_status
        FOREIGN KEY (to_status_id)
        REFERENCES complaint_statuses(status_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_csh_changed_by
        FOREIGN KEY (changed_by_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT ck_csh_status_change
        CHECK (from_status_id IS DISTINCT FROM to_status_id)
);

CREATE INDEX ix_status_history_complaint
    ON complaint_status_history(complaint_id, changed_at DESC);

-- 13 Escalation rules
CREATE TABLE IF NOT EXISTS escalation_rules (
    rule_id             INTEGER         GENERATED ALWAYS AS IDENTITY,
    rule_name           VARCHAR(150)    NOT NULL,
    priority_id         SMALLINT        NULL,
    category_id         INTEGER         NULL,
    escalation_level    SMALLINT        NOT NULL,
    trigger_after_hours SMALLINT        NOT NULL,
    escalate_to_role_id SMALLINT        NOT NULL,
    notify_by_email     BOOLEAN         NOT NULL DEFAULT TRUE,
    notify_by_inapp     BOOLEAN         NOT NULL DEFAULT TRUE,
    is_active           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_escalation_rules
        PRIMARY KEY (rule_id),

   

    CONSTRAINT fk_escalation_priority
        FOREIGN KEY (priority_id)
        REFERENCES priorities(priority_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT fk_escalation_category
        FOREIGN KEY (category_id)
        REFERENCES categories(category_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT fk_escalation_role
        FOREIGN KEY (escalate_to_role_id)
        REFERENCES roles(role_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT ck_escalation_level_positive
        CHECK (escalation_level > 0),

    CONSTRAINT ck_escalation_trigger_nonneg
        CHECK (trigger_after_hours >= 0),

    CONSTRAINT ck_escalation_name_nonempty
        CHECK (TRIM(rule_name) <> '')
);
 CREATE UNIQUE INDEX uix_escalation_rules
ON escalation_rules(
    COALESCE(priority_id, -1),
    COALESCE(category_id, -1),
    escalation_level
);
-- 14 Escalations
-- when escalation rules fire here escalation happens
-- 3 level of escalation - level1,2,3
CREATE TABLE IF NOT EXISTS escalations (
    escalation_id           BIGINT          GENERATED ALWAYS AS IDENTITY,
    complaint_id            BIGINT          NOT NULL,
    rule_id                 INTEGER         NOT NULL,
    escalation_level        SMALLINT        NOT NULL,
    escalated_to_employee_id INTEGER        NULL,
    escalated_at            TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    acknowledged_at         TIMESTAMPTZ     NULL,
    acknowledged_by         INTEGER         NULL,
    resolved_at             TIMESTAMPTZ     NULL,
    remarks                 TEXT            NULL,

    CONSTRAINT pk_escalations
        PRIMARY KEY (escalation_id),

    CONSTRAINT uq_escalations_complaint_level
        UNIQUE (complaint_id, escalation_level),

    CONSTRAINT fk_escalations_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON DELETE CASCADE ON UPDATE CASCADE,

    CONSTRAINT fk_escalations_rule
        FOREIGN KEY (rule_id)
        REFERENCES escalation_rules(rule_id)
        ON DELETE RESTRICT ON UPDATE CASCADE,

    CONSTRAINT fk_escalations_to_employee
        FOREIGN KEY (escalated_to_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT fk_escalations_ack_by
        FOREIGN KEY (acknowledged_by)
        REFERENCES employees(employee_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT ck_escalations_level_positive
        CHECK (escalation_level > 0),

    CONSTRAINT ck_escalations_ack_order
        CHECK (acknowledged_at IS NULL OR acknowledged_at >= escalated_at),

    CONSTRAINT ck_escalations_resolved_order
        CHECK (resolved_at IS NULL OR resolved_at >= escalated_at)
);

CREATE INDEX ix_escalations_complaint
    ON escalations(complaint_id);

CREATE INDEX ix_escalations_unresolved
    ON escalations(complaint_id)
    WHERE resolved_at IS NULL;

-- 15 Notifications
CREATE TABLE IF NOT EXISTS notifications (
    notification_id         BIGINT          GENERATED ALWAYS AS IDENTITY,
    recipient_employee_id   INTEGER         NOT NULL,
    complaint_id            BIGINT          NULL,
    notification_type       VARCHAR(50)     NOT NULL,
    subject                 VARCHAR(300)    NOT NULL,
    body                    TEXT            NOT NULL,
    channel                 VARCHAR(10)     NOT NULL DEFAULT 'IN_APP',
    is_sent                 BOOLEAN         NOT NULL DEFAULT FALSE,
    sent_at                 TIMESTAMPTZ     NULL,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_notifications
        PRIMARY KEY (notification_id),

    CONSTRAINT fk_notifications_recipient
        FOREIGN KEY (recipient_employee_id)
        REFERENCES employees(employee_id)
        ON DELETE CASCADE ON UPDATE CASCADE,

    CONSTRAINT fk_notifications_complaint
        FOREIGN KEY (complaint_id)
        REFERENCES complaints(complaint_id)
        ON DELETE SET NULL ON UPDATE CASCADE,

    CONSTRAINT ck_notifications_channel
        CHECK (channel IN ('IN_APP', 'EMAIL', 'BOTH')),

    CONSTRAINT ck_notifications_type
        CHECK (notification_type IN (
            'STATUS_CHANGE',       'ESCALATION',
            'COMMENT',             'SLA_BREACH',
            'ASSIGNMENT',          'RESOLUTION',
            'DEPT_HEAD_ESCALATION'
        )),

    CONSTRAINT ck_notifications_sent_order
        CHECK (sent_at IS NULL OR sent_at >= created_at)
);

CREATE INDEX ix_notifications_recipient_unsent
    ON notifications(recipient_employee_id, is_sent)
    WHERE is_sent = FALSE;

-- 16 Audit Logs table
CREATE TABLE IF NOT EXISTS audit_logs (
    audit_id                BIGINT          GENERATED ALWAYS AS IDENTITY,
    table_name              VARCHAR(100)    NOT NULL,
    record_id               VARCHAR(100)    NOT NULL,
    operation               CHAR(1)         NOT NULL,
    column_name             VARCHAR(100)    NOT NULL,
    old_value               TEXT            NULL,
    new_value               TEXT            NULL,
    changed_by_employee_id  INTEGER         NULL,
    changed_at              TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    application_context     VARCHAR(200)    NULL,

    CONSTRAINT pk_audit_logs
        PRIMARY KEY (audit_id),

    CONSTRAINT ck_audit_operation
        CHECK (operation IN ('I', 'U', 'D')),

    CONSTRAINT ck_audit_table_nonempty
        CHECK (TRIM(table_name) <> ''),

    CONSTRAINT ck_audit_record_nonempty
        CHECK (TRIM(record_id) <> ''),
    CONSTRAINT fk_audit_changed_by
FOREIGN KEY (changed_by_employee_id)
REFERENCES employees(employee_id)
ON DELETE SET NULL
ON UPDATE CASCADE
    
);


CREATE INDEX ix_audit_logs_table_record
    ON audit_logs(table_name, record_id);
CREATE INDEX ix_audit_logs_changed_by
    ON audit_logs(changed_by_employee_id)
    WHERE changed_by_employee_id IS NOT NULL;
CREATE INDEX ix_audit_logs_changed_at
    ON audit_logs(changed_at DESC);

-- FUNCTIONS AND TRIGGERS
-- 1
CREATE OR REPLACE FUNCTION fn_sync_primary_officer()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_actor_id  INTEGER;
    v_setting   TEXT;
BEGIN
    -- Only run when the assignment actually changes.
   IF TG_OP = 'UPDATE' THEN
    IF NEW.assigned_to_employee_id IS NOT DISTINCT FROM OLD.assigned_to_employee_id THEN
        RETURN NEW;
    END IF;
END IF;

    -- Resolve who is making this change (set by API per-request).
    v_setting := current_setting('cgr.current_employee_id', TRUE);
    IF v_setting IS NOT NULL AND v_setting <> '' THEN
        v_actor_id := v_setting::INTEGER;
    ELSE
        v_actor_id := NEW.assigned_to_employee_id; -- fallback: self-assign
    END IF;

    -- Deactivate the previous PRIMARY officer row (if any).
    UPDATE complaint_officers
       SET is_active = FALSE
     WHERE complaint_id       = NEW.complaint_id
       AND role_in_complaint  = 'PRIMARY'
       AND is_active          = TRUE;

    -- Insert or reactivate the new PRIMARY officer row.
    INSERT INTO complaint_officers
        (complaint_id, employee_id, role_in_complaint, assigned_at, assigned_by, is_active)
    VALUES
        (NEW.complaint_id, NEW.assigned_to_employee_id, 'PRIMARY', NOW(), v_actor_id, TRUE)
    ON CONFLICT (complaint_id, employee_id)
    DO UPDATE SET
        role_in_complaint = 'PRIMARY',
        is_active         = TRUE,
        assigned_at       = NOW(),
        assigned_by       = v_actor_id;

    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_complaints_sync_primary_officer
AFTER INSERT OR UPDATE OF assigned_to_employee_id ON complaints
FOR EACH ROW
WHEN (NEW.assigned_to_employee_id IS NOT NULL)
EXECUTE FUNCTION fn_sync_primary_officer();
-- Column-specific trigger — fires ONLY when assignment changes.

-- 2 Validate role request
CREATE OR REPLACE FUNCTION fn_validate_role_request()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_role_name VARCHAR(50);
BEGIN
    SELECT role_name INTO v_role_name
      FROM roles
     WHERE role_id = NEW.requested_role_id;

    IF v_role_name NOT IN ('GRIEVANCE_OFFICER', 'DEPARTMENT_HEAD') THEN
        RAISE EXCEPTION
            'Role requests are only permitted for GRIEVANCE_OFFICER and '
            'DEPARTMENT_HEAD. Requested role "%" is not allowed through '
            'this workflow.',
            COALESCE(v_role_name, 'UNKNOWN')
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_role_requests_validate_role
    BEFORE INSERT OR UPDATE OF requested_role_id ON role_requests
    FOR EACH ROW EXECUTE FUNCTION fn_validate_role_request();

-- 3 Enforce single admin

CREATE OR REPLACE FUNCTION fn_enforce_single_admin()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_admin_role_id SMALLINT;
    v_admin_count   INTEGER;
BEGIN
    -- Resolve the ADMIN role_id dynamically (not hardcoded).
    SELECT role_id INTO v_admin_role_id
      FROM roles
     WHERE role_name = 'ADMIN';

    -- Only relevant when the incoming row has the ADMIN role.
    IF NEW.role_id <> v_admin_role_id THEN
        RETURN NEW;
    END IF;

    -- On UPDATE: allow if it's the same admin updating themselves.
    IF TG_OP = 'UPDATE' AND OLD.employee_id = NEW.employee_id THEN
        RETURN NEW;
    END IF;

    -- Count existing active admin rows (excluding current row on UPDATE).
    SELECT COUNT(*) INTO v_admin_count
      FROM employees
     WHERE role_id   = v_admin_role_id
       AND is_active = TRUE
       AND (TG_OP = 'INSERT' OR employee_id <> OLD.employee_id);

    IF v_admin_count >= 1 THEN
        RAISE EXCEPTION
            'The system permits exactly one active ADMIN account. '
            'An active ADMIN already exists (employee_id: %). '
            'Deactivate the existing ADMIN before assigning this role.',
            (SELECT employee_id FROM employees
              WHERE role_id = v_admin_role_id AND is_active = TRUE
              LIMIT 1)
            USING ERRCODE = 'unique_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_employees_single_admin
    BEFORE INSERT OR UPDATE OF role_id, is_active ON employees
    FOR EACH ROW EXECUTE FUNCTION fn_enforce_single_admin();

-- 4 Enforce priority before assignment to officers
CREATE OR REPLACE FUNCTION fn_enforce_priority_before_assignment()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_display_order SMALLINT;
BEGIN
    -- Only run when status is actually changing.
    IF NEW.status_id IS NOT DISTINCT FROM OLD.status_id THEN
        RETURN NEW;
    END IF;

    -- Look up the display_order of the incoming status.
    SELECT display_order
      INTO v_display_order
      FROM complaint_statuses
     WHERE status_id = NEW.status_id;

    -- If moving past Submitted (display_order > 2) and no
    -- priority has been set, block the transition.
    IF v_display_order > 2 AND NEW.priority_id IS NULL THEN
        RAISE EXCEPTION
            'Complaint % cannot advance to status "%" (display_order=%) '
            'while priority_id is NULL. '
            'A Grievance Officer must set priority before assigning.',
            NEW.complaint_number,
            (SELECT status_name FROM complaint_statuses WHERE status_id = NEW.status_id),
            v_display_order
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_complaints_priority_gate
    BEFORE UPDATE OF status_id ON complaints
    FOR EACH ROW EXECUTE FUNCTION fn_enforce_priority_before_assignment();

-- 5 Shared before update trigger
CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_departments_updated_at
    BEFORE UPDATE ON departments
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_employees_updated_at
    BEFORE UPDATE ON employees
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_categories_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_complaints_updated_at
    BEFORE UPDATE ON complaints
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_comments_updated_at
    BEFORE UPDATE ON complaint_comments
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_escalation_rules_updated_at
    BEFORE UPDATE ON escalation_rules
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

CREATE TRIGGER trg_role_requests_updated_at
    BEFORE UPDATE ON role_requests
    FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

-- 6 Sync sla status
CREATE OR REPLACE FUNCTION fn_sync_sla_breached()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF  NEW.resolved_at  IS NULL
    AND NEW.sla_deadline IS NOT NULL
    AND NEW.sla_deadline < NOW() THEN
        NEW.sla_breached := TRUE;
    END IF;
    RETURN NEW;
END;
$$;


CREATE TRIGGER trg_complaints_sla_breached
    BEFORE INSERT OR UPDATE ON complaints
    FOR EACH ROW EXECUTE FUNCTION fn_sync_sla_breached();


CREATE OR REPLACE FUNCTION fn_validate_comment_parent()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    -- Prevent self-parenting
    IF NEW.parent_comment_id IS NOT NULL
       AND NEW.parent_comment_id = NEW.comment_id THEN
        RAISE EXCEPTION
            'A comment cannot reference itself as parent.'
            USING ERRCODE = 'check_violation';
    END IF;

    -- Prevent parent comment from belonging to another complaint
    IF NEW.parent_comment_id IS NOT NULL THEN

        IF NOT EXISTS (
            SELECT 1
            FROM complaint_comments pc
            WHERE pc.comment_id = NEW.parent_comment_id
              AND pc.complaint_id = NEW.complaint_id
        ) THEN
            RAISE EXCEPTION
                'Parent comment must belong to the same complaint.'
                USING ERRCODE = 'foreign_key_violation';
        END IF;

    END IF;

    RETURN NEW;
END;
$$;
CREATE TRIGGER trg_validate_comment_parent
BEFORE INSERT OR UPDATE OF parent_comment_id
ON complaint_comments
FOR EACH ROW
EXECUTE FUNCTION fn_validate_comment_parent();
-- View for complaint summary
CREATE OR REPLACE VIEW vw_complaint_summary AS
SELECT
    c.complaint_id,
    c.complaint_number,
    c.title,
    cat.category_name,
    p.priority_name,
    p.sla_multiplier,
    cs.status_name,
    cs.is_terminal,
    d.department_name,
    e_raised.full_name                                          AS raised_by_name,
    e_raised.email                                              AS raised_by_email,
    e_primary.full_name                                         AS assigned_to_name,
    co_primary.assigned_at                                      AS assigned_at,
    c.sla_deadline,
    c.sla_breached,
    c.created_at,
    c.updated_at,
    c.resolved_at,
    c.closed_at,
   CASE
    WHEN c.sla_deadline IS NULL THEN NULL
    ELSE ROUND(EXTRACT(EPOCH FROM (c.sla_deadline - NOW())) / 3600.0,2)
END AS sla_hours_remaining
FROM       complaints               c
JOIN       categories               cat         ON cat.category_id      = c.category_id
LEFT JOIN  priorities               p           ON p.priority_id        = c.priority_id
JOIN       complaint_statuses       cs          ON cs.status_id         = c.status_id
JOIN       departments              d           ON d.department_id      = c.department_id
JOIN       employees                e_raised    ON e_raised.employee_id = c.raised_by_employee_id
LEFT JOIN  complaint_officers       co_primary  ON co_primary.complaint_id    = c.complaint_id
                                               AND co_primary.role_in_complaint = 'PRIMARY'
                                               AND co_primary.is_active         = TRUE
LEFT JOIN  employees                e_primary   ON e_primary.employee_id = co_primary.employee_id;

