# REQUEST ROLE UPGRADE

## Purpose

Allows an employee to submit a request for a role upgrade within the organization hierarchy.

The system supports only the following upgrade paths:

```text
EMPLOYEE → GRO
GRO → DEPARTMENT_HEAD
```

The request is created with **PENDING** status and awaits approval by an authorized reviewer.

---

## Validation Checks

### Employee Validation

* Retrieve the employee using the logged-in Employee Id.
* Determine the employee's current role from the database.

**Purpose**

* Prevent stale token issues by using the latest role information stored in the system.

---

### Role Upgrade Path Validation

* Validate that the requested role is a valid upgrade from the employee's current role.

#### Allowed Upgrade Paths

```text
EMPLOYEE → GRO
GRO → DEPARTMENT_HEAD
```

#### Invalid Upgrade Examples

```text
EMPLOYEE → DEPARTMENT_HEAD
DEPARTMENT_HEAD → ADMIN
GRO → EMPLOYEE
ADMIN → ANY ROLE
```

**Failure Condition**

* If the requested role does not represent a valid upgrade path, throw a **Business Rule Exception**.

**Message**

```text
Invalid role upgrade path. Only EMPLOYEE to GRO or GRO to DEPARTMENT_HEAD upgrades are allowed.
```

---

### Existing Pending Request Validation

* Check whether the employee already has a pending role upgrade request.

**Failure Condition**

* If a pending request already exists, throw a **Conflict Exception**.

**Message**

```text
You already have a pending role upgrade request.
```

---

## Create Role Upgrade Request

Create a new record in **Role_Request** containing:

* Employee Id
* Current Role Id
* Requested Role Id
* Request Status = PENDING
* Created At Timestamp

---

## Request Status

### Initial Status

```text
PENDING
```

The request remains pending until reviewed and either approved or rejected by the appropriate authority.

---

## Save Request

1. Create the role request record.
2. Set Request Status to **PENDING**.
3. Set Current Role Id from the employee's latest role in the database.
4. Set Requested Role Id from the request DTO.
5. Set CreatedAt to current UTC timestamp.
6. Persist the request.

---

## Response

After successful creation:

1. Reload the employee's role requests.
2. Retrieve the newly created request.
3. Map the request entity to **RoleRequestDto**.
4. Return the created request details.

---

## Business Rules

* Employees can request only valid role upgrades.
* Only the following upgrade paths are allowed:

  * EMPLOYEE → GRO
  * GRO → DEPARTMENT_HEAD
* Invalid upgrade paths are rejected.
* An employee can have only one pending role upgrade request at a time.
* Current role must always be retrieved from the database to avoid stale token issues.
* Every newly created request starts with **PENDING** status.

---

## Request Flow

```text
EMPLOYEE
    │
    ▼
Request GRO Role
    │
    ▼
PENDING
    │
    ├── APPROVED → GRO
    │
    └── REJECTED
```

```text
GRO
    │
    ▼
Request Department Head Role
    │
    ▼
PENDING
    │
    ├── APPROVED → DEPARTMENT_HEAD
    │
    └── REJECTED
```

---

## Status Flow

```text
PENDING → APPROVED
PENDING → REJECTED
```
# REJECT ROLE UPGRADE REQUEST

## Purpose

Allows an authorized administrator to reject a pending role upgrade request submitted by an employee.

Upon rejection, the request status is updated to **REJECTED**, review details are recorded, and the updated request information is returned.

---

## Validation Checks

### Role Request Existence Validation

* Retrieve the role request using the provided Role Request Id.

**Failure Condition**

* If the role request does not exist, throw a **Not Found Exception**.

**Message**

```text id="rj1"
Role request not found.
```

---

### Request Status Validation

* Verify that the role request is currently in **PENDING** status.
* Only pending requests can be reviewed and rejected.

**Failure Condition**

* If the request has already been reviewed (APPROVED or REJECTED), throw a **Business Rule Exception**.

**Message**

```text id="rj2"
This request has already been reviewed.
```

---

## Status Transition

### Allowed Transition

```text id="rj3"
PENDING → REJECTED
```

---

## Update Role Request

1. Set **Request Status = REJECTED**.
2. Set **Reviewed By** to the current Admin Id.
3. Set **Reviewed At** to the current UTC timestamp.
4. Store rejection remarks provided by the administrator.
5. Save the updated request.

---

## Review Information Recorded

The following review details are stored:

| Field          | Value                 |
| -------------- | --------------------- |
| Request Status | REJECTED              |
| Reviewed By    | Admin Id              |
| Reviewed At    | Current UTC Timestamp |
| Remarks        | Rejection Remarks     |

---

## Save Changes

Persist the updated role request record with the review information.

---

## Response

After successful rejection:

1. Retrieve all role requests for the employee.
2. Locate the updated request.
3. Map the request entity to **RoleRequestDto**.
4. Return the updated request details.

---

## Business Rules

* Only pending requests can be rejected.
* Approved or previously rejected requests cannot be reviewed again.
* Rejection must record:

  * Reviewer Id
  * Review Timestamp
  * Rejection Remarks (if provided)
* Rejection does not change the employee's current role.
* The request remains available for audit and historical tracking.

---

## Request Flow

```text id="rj4"
Employee Submits Request
            │
            ▼
         PENDING
            │
            ▼
        REJECTED
```

---

## Status Flow

```text id="rj5"
PENDING → REJECTED
```

---

## Audit Information

The following information is retained for auditing purposes:

* Role Request Id
* Employee Id
* Current Role Id
* Requested Role Id
* Request Status
* Reviewed By
* Reviewed At
* Remarks

This ensures complete traceability of role request review actions.
# APPROVE ROLE UPGRADE REQUEST

## Purpose

Allows an authorized administrator to approve a pending role upgrade request submitted by an employee.

Upon approval:

* The employee's role is upgraded.
* A department may be assigned if required.
* Department Head assignments are validated and updated.
* The request status is changed to **APPROVED**.
* All changes are committed within a database transaction.

---

## Validation Checks

### Role Request Existence Validation

* Retrieve the role request using the provided Role Request Id.

**Failure Condition**

* If the role request does not exist, throw a **Not Found Exception**.

**Message**

```text id="apr1"
Role request not found.
```

---

### Request Status Validation

* Verify that the role request is currently in **PENDING** status.

**Failure Condition**

* If the request has already been reviewed, throw a **Business Rule Exception**.

**Message**

```text id="apr2"
This request has already been reviewed.
```

---

### Employee Validation

* Retrieve the employee associated with the role request.
* Determine the employee's current department assignment.

---

### Department Assignment Validation

A role upgrade requires the employee to belong to a department.

#### Existing Department

* If the employee already belongs to a department, use that department.

#### Department Assignment Required

* If the employee is not assigned to a department, the administrator must provide a department through the approval request.

**Failure Condition**

* If no department exists and no department is provided, throw a **Validation Exception**.

**Message**

```text id="apr3"
Department assignment is required.
```

---

### Department Validation

* Retrieve the target department using the resolved Department Id.

---

## Department Head Eligibility Validation

Additional validation is required when approving a request for **Department Head** role.

### Existing Department Head Validation

* Verify whether the department already has a Department Head assigned.

**Failure Condition**

* If another employee is already assigned as Department Head of the department, throw a **Business Rule Exception**.

**Message**

```text id="apr4"
Department already has a department head.
```

---

### Single Department Head Ownership Validation

* Verify that the employee is not already assigned as Department Head of another department.

**Failure Condition**

* If the employee is already a Department Head in another department, throw a **Business Rule Exception**.

**Message**

```text id="apr5"
Employee is already department head of another department.
```

---

## Status Transition

### Allowed Transition

```text id="apr6"
PENDING → APPROVED
```

---

## Update Employee

Upon successful validation:

1. Update Employee Role to the requested role.
2. Assign the resolved Department Id.
3. Update Employee UpdatedAt timestamp.
4. Save employee changes.

---

## Department Head Assignment

### Applicable Condition

* Execute only when the approved role is **DEPARTMENT_HEAD**.

### Actions

1. Assign Employee as Department Head of the department.
2. Update DepartmentHeadEmployeeId.
3. Save department changes.

---

## Update Role Request

After successful employee update:

1. Set Request Status = APPROVED.
2. Set Reviewed By = Admin Id.
3. Set Reviewed At = Current UTC Timestamp.
4. Store approval remarks.
5. Save request changes.

---

## Review Information Recorded

| Field          | Value                 |
| -------------- | --------------------- |
| Request Status | APPROVED              |
| Reviewed By    | Admin Id              |
| Reviewed At    | Current UTC Timestamp |
| Remarks        | Approval Remarks      |

---

## Save Changes

Persist all updates:

* Employee
* Department (if applicable)
* Role Request

---

## Response

After successful approval:

1. Retrieve the employee's role requests.
2. Locate the approved request.
3. Map the request entity to **RoleRequestDto**.
4. Return the updated request details.

---

## Business Rules

* Only pending requests can be approved.
* Approved or rejected requests cannot be reviewed again.
* Employees must belong to a department after role approval.
* Department assignment is mandatory when the employee has no department.
* A department can have only one Department Head.
* An employee can be Department Head of only one department.
* Approving a request updates the employee's role.
* Approving a Department Head request updates the department's Department Head assignment.
* Approval information must be recorded for audit purposes.
* All operations must execute within a database transaction.

---

## Request Flow

### Employee → GRO

```text id="apr7"
EMPLOYEE
    │
    ▼
Request GRO Role
    │
    ▼
PENDING
    │
    ▼
APPROVED
    │
    ▼
GRO
```

---

### GRO → Department Head

```text id="apr8"
GRO
    │
    ▼
Request Department Head Role
    │
    ▼
PENDING
    │
    ▼
APPROVED
    │
    ▼
DEPARTMENT_HEAD
```

---

## Status Flow

```text id="apr9"
PENDING → APPROVED
```

---

## Transaction Handling

The entire approval process executes within a database transaction.

### Commit

The transaction is committed when:

* Employee role update succeeds.
* Department update succeeds (if applicable).
* Role request update succeeds.

### Rollback

The transaction is rolled back if any exception occurs during processing.

This ensures that no partial role upgrades or department assignments are persisted.

---

## Audit Information

The following information is retained for auditing purposes:

* Role Request Id
* Employee Id
* Previous Role
* Approved Role
* Department Assignment
* Reviewed By
* Reviewed At
* Approval Remarks
* Request Status

This ensures complete traceability of all role approval actions.
# MANUAL ROLE CHANGE

## Purpose

Allows an administrator to directly change an employee's role without requiring a role upgrade request workflow.

The operation:

* Updates the employee's role immediately.
* Handles department assignment requirements.
* Manages Department Head assignments.
* Creates an approved role request record for audit purposes.
* Executes within a database transaction.

---

## Validation Checks

### Employee Validation

* Retrieve the employee using the provided Employee Id.

---

### Admin Role Protection Validation

* Verify that the employee's current role is not **ADMIN**.

**Failure Condition**

* If the employee currently has the ADMIN role, throw a **Business Rule Exception**.

**Message**

```text id="mrc1"
Admin role cannot be changed.
```

---

### Target Role Validation

* Verify that the target role is not ADMIN.

**Failure Condition**

* If the target role is ADMIN, throw a **Business Rule Exception**.

**Message**

```text id="mrc2"
Cannot assign ADMIN role.
```

---

### Duplicate Role Validation

* Verify that the employee does not already have the requested role.

**Failure Condition**

* If the employee already has the target role, throw a **Validation Exception**.

**Message**

```text id="mrc3"
Employee already has this role.
```

---

### Allowed Role Validation

Only the following target roles are supported:

```text id="mrc4"
EMPLOYEE
GRO
DEPARTMENT_HEAD
```

**Failure Condition**

* If the target role is not one of the supported roles, throw a **Validation Exception**.

**Message**

```text id="mrc5"
Invalid target role.
```

---

## Department Resolution

### Determine Department Assignment

* Use the employee's existing department if available.
* If the employee has no department and a department is supplied in the request, assign the provided department.

---

### Department Requirement Validation

Department assignment is mandatory when assigning:

* GRO
* DEPARTMENT_HEAD

**Failure Condition**

* If the target role requires a department and no department is available, throw a **Validation Exception**.

**Message**

```text id="mrc6"
Department assignment is required.
```

---

### Department Validation

* Retrieve the department using the resolved Department Id.

---

## Department Head Eligibility Validation

Additional validation is required when assigning the **DEPARTMENT_HEAD** role.

### Department Assignment Validation

**Failure Condition**

* If no department can be resolved, throw a **Validation Exception**.

**Message**

```text id="mrc7"
Department assignment is required.
```

---

### Existing Department Head Validation

* Verify that the department does not already have a different Department Head assigned.

**Failure Condition**

* If another employee is already assigned as Department Head, throw a **Business Rule Exception**.

**Message**

```text id="mrc8"
Department already has a department head.
```

---

### Multiple Department Head Validation

* Verify that the employee is not already serving as Department Head for another department.

**Failure Condition**

* If the employee is already the Department Head of another department, throw a **Business Rule Exception**.

**Message**

```text id="mrc9"
Employee is already department head of another department.
```

---

## Existing Department Head Removal

### Applicable Condition

* The employee is currently a Department Head.
* The target role is not Department Head.

### Actions

1. Locate the department headed by the employee.
2. Remove the employee from the Department Head assignment.
3. Update the department record.

---

## Update Employee

Store the employee's current role as **Old Role**.

Update:

1. Role Id = Target Role
2. Department Id = Resolved Department
3. UpdatedAt = Current UTC Timestamp

Save employee changes.

---

## Assign Department Head

### Applicable Condition

* Target Role = DEPARTMENT_HEAD

### Actions

1. Assign employee as Department Head of the department.
2. Update DepartmentHeadEmployeeId.
3. Save department changes.

---

## Create Audit Role Request Record

To maintain role change history, create a Role Request record with the following values:

| Field             | Value                 |
| ----------------- | --------------------- |
| Employee Id       | Employee              |
| Current Role Id   | Previous Role         |
| Requested Role Id | Target Role           |
| Request Status    | APPROVED              |
| Reviewed By       | Admin                 |
| Reviewed At       | Current UTC Timestamp |
| Remarks           | Admin Remarks         |
| Created At        | Current UTC Timestamp |

---

## Status Handling

Since this is a direct administrative action, the generated role request record is automatically marked as:

```text id="mrc10"
APPROVED
```

No approval workflow is required.

---

## Save Changes

Persist:

* Employee updates
* Department updates (if applicable)
* Audit role request record

---

## Response

After successful completion:

1. Retrieve employee role request history.
2. Locate the newly created role request record.
3. Map the record to **RoleRequestDto**.
4. Return the role change details.

---

## Business Rules

* ADMIN users cannot have their roles changed.
* ADMIN role cannot be assigned through manual role change.
* Employee cannot be assigned to the same role they already possess.
* Only EMPLOYEE, GRO, and DEPARTMENT_HEAD roles are valid targets.
* GRO and DEPARTMENT_HEAD roles require department assignment.
* A department can have only one Department Head.
* An employee can be Department Head of only one department.
* When a Department Head is downgraded, the Department Head assignment is removed automatically.
* Manual role changes create an audit role request record with APPROVED status.
* All updates execute within a database transaction.

---

## Role Change Flow

### Employee to GRO

```text id="mrc11"
EMPLOYEE
    │
    ▼
Manual Role Change
    │
    ▼
GRO
```

---

### GRO to Department Head

```text id="mrc12"
GRO
    │
    ▼
Manual Role Change
    │
    ▼
DEPARTMENT_HEAD
```

---

### Department Head to GRO / Employee

```text id="mrc13"
DEPARTMENT_HEAD
       │
       ▼
Remove Department Head Assignment
       │
       ▼
GRO or EMPLOYEE
```

---

## Transaction Handling

The entire operation executes within a database transaction.

### Commit

The transaction is committed when:

* Employee update succeeds.
* Department updates succeed.
* Audit role request creation succeeds.

### Rollback

The transaction is rolled back if any exception occurs during processing.

This prevents partial role updates and inconsistent department assignments.

---

## Audit Information

The following information is retained for auditing purposes:

* Employee Id
* Previous Role
* New Role
* Department Assignment
* Reviewed By
* Reviewed At
* Remarks
* Request Status = APPROVED

This provides a complete audit trail for all manual role changes performed by administrators.
