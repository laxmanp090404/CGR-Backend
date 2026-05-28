INSERT INTO departments (department_name, parent_dept_id, head_employee_id, is_active)
VALUES ('Administration', NULL, NULL, TRUE);

INSERT INTO roles (role_name, description) VALUES
    ('EMPLOYEE',          'Can raise and track own complaints'),
    ('GRIEVANCE_OFFICER', 'Handles, assigns, and resolves complaints at operational level'),
    ('DEPARTMENT_HEAD',   'Receives Level-2 escalations; can override assignment and resolution within their department'),
    ('ADMIN',             'Full system access and configuration');

INSERT INTO employees (
    full_name,
    email,
    mobile_number,
    address_encrypted,
    password_hash,
    department_id,
    role_id,
    is_active
)
SELECT
    'System Administrator',
    'admin@cgr.internal',
    NULL,
    NULL,
    '$2a$12$nNfjSuqOzpkLZuPP60.xL.yapRQft2bQJ6Z95UROjbzRQBz9XzGPa',
    d.department_id,
    r.role_id,
    TRUE
FROM  departments d
CROSS JOIN roles  r
WHERE d.department_name = 'Administration'
  AND r.role_name       = 'ADMIN';


INSERT INTO priorities (priority_name, sla_multiplier, escalation_hours) VALUES
    ('Low',      2.00, 72),
    ('Medium',   1.00, 48),
    ('High',     0.50, 24),
    ('Critical', 0.25,  8);


INSERT INTO complaint_statuses (status_name, is_terminal, display_order) VALUES
    ('Draft',       FALSE, 1),
    ('Submitted',   FALSE, 2),
    ('Assigned',    FALSE, 3),
    ('In Progress', FALSE, 4),
    ('Resolved',    FALSE, 5),
    ('Closed',      TRUE,  6),
    ('Rejected',    TRUE,  7);