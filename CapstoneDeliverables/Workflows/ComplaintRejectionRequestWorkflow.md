# CREATE COMPLAINT REJECTION REQUEST

## Purpose

Allows the current complaint handler (GRO or Department Head) to submit a **Rejection Request** when they believe a complaint should be rejected instead of being processed further.

The request is created with **PENDING** status and awaits administrative review.

---

## Validation Checks

### Authentication Validation

* Verify that the user is authenticated.

**Failure Condition**

* If the user is not authenticated, throw an **Unauthorized Access Exception**.

---

### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.

**Failure Condition**

* If the complaint does not exist, throw a **Not Found Exception**.

**Message**

```text id="crr1"
Complaint {ComplaintId}
```

---

### Current Handler Validation

* Verify that the logged-in employee is the current handler of the complaint.

**Failure Condition**

* If the logged-in employee is not the current handler, throw a **Forbidden Exception**.

**Message**

```text id="crr2"
Only the current handler can raise a rejection request.
```

---

### Role Validation

* Verify that the logged-in employee has one of the following roles:

  * GRO
  * DEPARTMENT_HEAD

**Failure Condition**

* If the employee has any other role, throw a **Forbidden Exception**.

**Message**

```text id="crr3"
Only GROs and Department Heads can create rejection requests.
```

---

### Request Type Validation

* Verify that the request type is **REJECTION**.

**Failure Condition**

* If any other request type is supplied, throw a **Business Rule Exception**.

**Message**

```text id="crr4"
Only rejection requests are supported.
```

---

### Complaint Status Validation

Rejection requests can only be created when the complaint is currently in one of the following statuses:

* ASSIGNED
* IN_PROGRESS
* ESCALATED

**Failure Condition**

* If the complaint is in any other status, throw a **Business Rule Exception**.

**Message**

```text id="crr5"
Rejection request cannot be created for this complaint status.
```

---

### Existing Pending Request Validation

* Check whether a pending rejection request already exists for the complaint.

**Failure Condition**

* If a pending rejection request already exists, throw a **Conflict Exception**.

**Message**

```text id="crr6"
A pending rejection request already exists.
```

---

## Status Transition

### Request Status

All newly created rejection requests begin with:

```text id="crr7"
PENDING
```

---

## Create Rejection Request

Create a new record in **Complaint_Request** containing:

* Complaint Id
* Request Type = REJECTION
* Requested By Employee Id
* Request Status = PENDING
* Remarks
* Created At Timestamp

---

## Save Request

1. Create the complaint request record.
2. Set Request Type to **REJECTION**.
3. Set Requested By to the current employee.
4. Set Request Status to **PENDING**.
5. Store request remarks.
6. Set CreatedAt to current UTC timestamp.
7. Persist the request.

---

## Response

After successful creation:

1. Reload the newly created request.
2. Map the entity to **ComplaintRequestDto**.
3. Return the request details.

---

## Audit Logging

After successful creation, an application log entry is generated:

```text id="crr8"
Rejection request {RequestId} created for Complaint {ComplaintId}
```

---

## Business Rules

* Only authenticated users can create rejection requests.
* Only the current complaint handler can submit a rejection request.
* Only GROs and Department Heads can create rejection requests.
* Only REJECTION request type is supported.
* Rejection requests can only be created when the complaint is in:

  * ASSIGNED
  * IN_PROGRESS
  * ESCALATED
* Only one pending rejection request can exist per complaint at a time.
* Every newly created request starts with **PENDING** status.
* Remarks may be provided to justify the rejection request.

---

## Request Flow

```text id="crr9"
ASSIGNED / IN_PROGRESS / ESCALATED
                │
                ▼
      Create Rejection Request
                │
                ▼
             PENDING
                │
        Await Review
```

---

## Status Flow

```text id="crr10"
PENDING → APPROVED
PENDING → REJECTED
```

(Approval and rejection are handled through the request review process.)
# REVIEW COMPLAINT REJECTION REQUEST

## Purpose

Allows an administrator to review a pending **Complaint Rejection Request** submitted by a GRO or Department Head.

The administrator may:

* **Approve** the rejection request, resulting in the complaint being rejected.
* **Reject** the rejection request, resulting in the complaint being reopened, reprioritized, and reassigned.

All operations are executed within a database transaction to ensure consistency.

---

## Validation Checks

### Authentication Validation

* Verify that the user is authenticated.

**Failure Condition**

* If the user is not authenticated, throw an **Unauthorized Access Exception**.

---

### Admin Authorization Validation

* Verify that the logged-in user has the **ADMIN** role.

**Failure Condition**

* If the user is not an Admin, throw a **Forbidden Exception**.

**Message**

```text id="rcr1"
Only admins can review complaint requests.
```

---

### Request Existence Validation

* Retrieve the complaint request using the provided Request Id.

**Failure Condition**

* If the request does not exist, throw a **Not Found Exception**.

**Message**

```text id="rcr2"
Complaint request {RequestId} not found
```

---

### Request Type Validation

* Verify that the request type is **REJECTION**.

**Failure Condition**

* If the request is not a rejection request, throw a **Business Rule Exception**.

**Message**

```text id="rcr3"
Only rejection requests can be reviewed.
```

---

### Request Status Validation

* Verify that the request is currently **PENDING**.

**Failure Condition**

* If the request has already been reviewed, throw a **Business Rule Exception**.

**Message**

```text id="rcr4"
This request has already been reviewed.
```

---

## Review Request

The request is updated with:

* Reviewed By = Current Admin
* Reviewed At = Current UTC Timestamp
* Request Status

  * APPROVED (if dto.Approve = true)
  * REJECTED (if dto.Approve = false)
* Review Remarks

---

## Status Transition

### Request Status Flow

```text id="rcr5"
PENDING → APPROVED
PENDING → REJECTED
```

---

## Scenario 1: Rejection Request Approved

## Purpose

The administrator agrees that the complaint should be rejected.

---

## Complaint Status Transition

```text id="rcr6"
ASSIGNED / IN_PROGRESS / ESCALATED
                    ↓
                REJECTED
```

---

## Update Complaint

1. Set Complaint Status = REJECTED.
2. Update UpdatedAt timestamp.
3. Save complaint changes.

---

## Create Complaint History

Insert a Complaint History record containing:

* Complaint Id
* Previous Status
* New Status = REJECTED
* Current Handler
* Reviewed By (Admin)
* Remarks
* Escalation Level Snapshot
* Timestamp

---

## Notify Complaint Creator

Send notification to the employee who raised the complaint.

### Details

| Field     | Value              |
| --------- | ------------------ |
| Type      | Complaint Rejected |
| Title     | Complaint Rejected |
| Recipient | Complaint Creator  |
| Reference | Complaint Id       |

### Message

```text id="rcr7"
Your complaint '{Complaint Title}' has been rejected.
```

---

## Audit Logging

```text id="rcr8"
Rejection request {RequestId} approved by Admin {AdminId}
```

---

## Scenario 2: Rejection Request Rejected

## Purpose

The administrator disagrees with the rejection request.

The complaint is reopened and reassigned for further processing.

---

## Reopen Eligibility Validation

### Maximum Reopen Limit Validation

* Verify that the complaint has not reached the maximum reopen limit.

**Failure Condition**

* If ReopenedCount >= 3, throw a **Business Rule Exception**.

**Message**

```text id="rcr9"
Cannot reopen complaint. Maximum reopen limit reached. Approve the rejection instead.
```

---

## Priority Escalation

To compensate for the delay caused by the rejected rejection request:

* Increase complaint priority by one level.
* Do not increase priority beyond CRITICAL.

### Example

```text id="rcr10"
LOW      → MEDIUM
MEDIUM   → HIGH
HIGH     → CRITICAL
CRITICAL → CRITICAL
```

---

## Determine New Assignment

1. Retrieve the employee who originally raised the complaint.
2. Invoke the Assignment Engine.
3. Determine the appropriate handler and escalation level.

---

## Reopen Complaint

1. Increment ReopenedCount.
2. Set Status = REOPENED.

---

## Create Reopen History

Insert Complaint History:

* Previous Status
* New Status = REOPENED
* Existing Handler
* Admin Reviewer
* Escalation Snapshot
* Timestamp

Remarks:

```text id="rcr11"
Rejection request rejected. Complaint reopened.
```

---

## Reassign Complaint

After reopening:

1. Assign complaint to the handler determined by the Assignment Engine.
2. Set Status = ASSIGNED.
3. Update Escalation Level.
4. Recalculate Escalation Due Date.
5. Update UpdatedAt timestamp.
6. Save complaint.

---

## Create Assignment History

Insert Complaint Assignment History:

* Complaint Id
* Old Handler
* New Handler
* Assignment Reason = REOPEN

---

## Create Reassignment History

Insert Complaint History:

* Previous Status = REOPENED
* New Status = ASSIGNED
* Old Handler
* New Handler
* Escalation Level Snapshot
* Timestamp

Remarks:

```text id="rcr12"
Complaint reassigned after reopen.
```

---

## Complaint Status Flow

```text id="rcr13"
ASSIGNED / IN_PROGRESS / ESCALATED
                    ↓
                REOPENED
                    ↓
                ASSIGNED
```

---

## Notify New Handler

### Details

| Field     | Value              |
| --------- | ------------------ |
| Type      | Complaint Reopened |
| Title     | Complaint Assigned |
| Recipient | New Handler        |
| Reference | Complaint Id       |

### Message

```text id="rcr14"
Complaint '{Complaint Title}' has been reopened and assigned to you.
```

---

## Notify Complaint Creator

### Details

| Field     | Value              |
| --------- | ------------------ |
| Type      | Complaint Reopened |
| Title     | Complaint Reopened |
| Recipient | Complaint Creator  |
| Reference | Complaint Id       |

### Message

```text id="rcr15"
Your complaint '{Complaint Title}' has been reopened.
```

---

## Business Rules

* Only Admins can review complaint rejection requests.
* Only pending rejection requests can be reviewed.
* Approving a rejection request causes the complaint to become REJECTED.
* Rejecting a rejection request causes the complaint to be reopened.
* A complaint may be reopened a maximum of 3 times.
* Reopened complaints receive a priority increase (up to CRITICAL).
* Reopened complaints are reassigned through the Assignment Engine.
* Every review action must update:

  * Complaint Request
  * Complaint History
* Reopened complaints must additionally create:

  * Assignment History
  * Reassignment History
* Notifications must be sent to affected users.
* All operations execute within a database transaction.

---

## Transaction Handling

### Commit

The transaction is committed when:

* Request review update succeeds.
* Complaint updates succeed.
* History records are created successfully.
* Assignment records are created successfully.
* Notifications are sent successfully.

### Rollback

If any exception occurs:

* The transaction is rolled back.
* No partial changes are persisted.

---

## Complete Flow

```text id="rcr16"
Rejection Request
        │
        ▼
      PENDING
        │
 ┌──────┴──────┐
 │             │
 ▼             ▼
APPROVED    REJECTED
 │             │
 ▼             ▼
Complaint   Complaint
REJECTED    REOPENED
                │
                ▼
            ASSIGNED
```
