#### COMPLAINT STATUSES

* **SUBMITTED**
* **ASSIGNED**
* **IN_PROGRESS**
* **ESCALATED**
* **RESOLVED**
* **CLOSED**
* **REJECTED**
* **REOPENED**
* **EXTERNALLY_ESCALATED**

---
# Complaint Workflows:

#### CREATE COMPLAINT

1. **Role Validation**

   * Verify the complaint creator's role.
   * **Admin users cannot create complaints.**
   * Only authorized roles (Employee, GRO, Department Head) can raise complaints.

2. **Category Validation**

   * Verify that the selected complaint category is **active**.
   * Reject complaint creation if the category is inactive.

3. **Duplicate Complaint Check**

   * Check whether an identical complaint has already been created by the same user within the last **2 minutes**.
   * Prevent creation of duplicate complaints to avoid spam submissions.

4. **Create Complaint**

   * Create the complaint with status **SUBMITTED**.
   * Initialize complaint metadata, priority, category, creator details, and timestamps.

5. **Create Complaint History**

   * Insert an entry into the **Complaint_History** table recording the complaint creation event.
   * Status recorded as **SUBMITTED**.

6. **Determine Initial Assignee**

   * Execute the complaint assignment algorithm.

   **If complaint is created by an Employee:**

   * Retrieve active GROs belonging to the complaint category's department.
   * Exclude GROs who have previously handled the same complaint.
   * Assign to the first available eligible GRO with the lowest workload.
   * Set escalation level to **0**.

   **If no eligible GRO is available:**

   * Assign to the Department Head.
   * Set escalation level to **1**.

   **If no Department Head exists:**

   * Assign to the Admin.
   * Set escalation level to **2**.

   **If complaint is created by a GRO:**

   * Assign directly to the Department Head.
   * Escalation level = **1**.
   * If no Department Head exists, assign to Admin with escalation level **2**.

   **If complaint is created by a Department Head:**

   * Assign directly to the Admin.
   * Escalation level = **2**.

7. **Update Complaint Status**

   * Change complaint status from **SUBMITTED** to **ASSIGNED**.

8. **Calculate Escalation Due Date**

   * For escalation level **0**, calculate escalation due time using the category SLA hours.
   * For escalation levels **1** and above, calculate escalation due time using the configured escalation rules based on:

     * Category
     * Priority
     * Escalation Level

9. **Create Assignment History**

   * Insert a record into the **Complaint_Assignment_History** table containing:

     * Complaint Id
     * Assigned Handler
     * Escalation Level
     * Assignment Timestamp
     * Escalation Due Timestamp

10. **Send Notifications**

    * Notify the assigned handler about the new complaint.
    * Notify the complaint creator that the complaint has been assigned.

---

#### AUTOMATIC ESCALATION

When a complaint reaches its escalation due time without resolution:

**Escalation Level 0 → Level 1**

* Reassign complaint from GRO to Department Head.
* Update escalation level to **1**.
* Create assignment history entry.
* Recalculate next escalation due time.
* Send notifications.

**Escalation Level 1 → Level 2**

* Reassign complaint from Department Head to Admin.
* Update escalation level to **2**.
* Create assignment history entry.
* Recalculate next escalation due time.
* Send notifications.

**Escalation Level 2 → Level 3**

* Mark complaint as **EXTERNALLY_ESCALATED**.
* Set handler id to **0** (external authority).
* Update escalation level to **3**.
* Create assignment history entry.
* Send notifications.

**Invalid Escalation Level**

* Throw a business rule exception indicating an invalid escalation state.

---

#### ESCALATION LEVELS

| Escalation Level | Assigned Authority |
| ---------------- | ------------------ |
| 0                | GRO                |
| 1                | Department Head    |
| 2                | Admin              |
| 3                | External Authority |

---

#### FALLBACK ASSIGNMENT RULES

* If no eligible GRO is available, assign to the Department Head.
* If no Department Head exists, assign to the Admin.
* If no Admin exists, throw a business rule exception.
* A complaint should never remain unassigned after successful creation.

# ComplaintAttachmentService Logic

## ATTACHMENT RULES

### Allowed File Types

* PNG (`image/png`)
* JPG (`image/jpg`)
* JPEG (`image/jpeg`)
* PDF (`application/pdf`)

### File Limits

| Rule                              | Limit |
| --------------------------------- | ----- |
| Maximum files per complaint       | 3     |
| Maximum image size (PNG/JPG/JPEG) | 3 MB  |
| Maximum PDF size                  | 5 MB  |
| Maximum total attachment size     | 15 MB |

---

## SAVE ATTACHMENTS

### Validation Checks

#### File Count Validation

* Verify that the number of uploaded files does not exceed **3 files**.
* If exceeded, throw a business rule exception.

#### Total Size Validation

* Calculate the combined size of all uploaded files.
* If the total size exceeds **15 MB**, reject the upload.

#### File Type Validation

* Verify that each file's MIME type is one of the allowed formats:

  * PNG
  * JPG
  * JPEG
  * PDF

* If an unsupported file type is detected, throw a validation exception.

#### Individual File Size Validation

##### Image Files

* Maximum allowed size: **3 MB**
* Applicable for:

  * PNG
  * JPG
  * JPEG

##### PDF Files

* Maximum allowed size: **5 MB**

* If the file exceeds the permitted size, throw a validation exception.

---

### File Storage Process

1. Create a complaint-specific folder using:

   ```text
   {ComplaintAttachmentsPath}/{ComplaintId}
   ```

2. Generate a unique file name using a GUID.

3. Save the file to the complaint folder.

4. Track created file paths for rollback or cleanup if required.

---

### Attachment Record Creation

For each successfully uploaded file:

* Create a ComplaintAttachment record containing:

  * Complaint Id
  * Original File Name
  * Stored File Path
  * MIME Type
  * File Size
  * Upload Timestamp
  * Uploaded By Employee Id

* Persist the attachment record in the database.

---

### Return Result

Return:

* List of created attachment records.
* List of physical files created on disk.

---

## GET ATTACHMENTS BY COMPLAINT

### Retrieve Attachments

1. Fetch all attachments associated with the complaint.
2. Map attachment entities to DTOs.
3. Return attachment details including:

   * Attachment Id
   * Complaint Id
   * Original File Name
   * MIME Type
   * File Size
   * File Path
   * Uploaded By
   * Uploaded By Name
   * Upload Timestamp

---

## DELETE SPECIFIC FILES

### File Cleanup Process

1. Iterate through the provided file paths.
2. Delete each file if it exists.
3. Track the complaint folder location.

### Empty Folder Cleanup

After file deletion:

* Check whether the complaint folder is empty.
* If no files remain, delete the folder.

---

## DELETE COMPLAINT FOLDER

### Complete Folder Removal

1. Locate the complaint-specific attachment directory.

   ```text
   {ComplaintAttachmentsPath}/{ComplaintId}
   ```

2. Verify that the directory exists.

3. Delete the directory and all its contents recursively.

---

## STORAGE STRUCTURE

```text
ComplaintAttachments/
└── {ComplaintId}/
    ├── {Guid1}.jpg
    ├── {Guid2}.pdf
    └── {Guid3}.png
```

---

## BUSINESS RULES

* Maximum of 3 attachments per complaint.
* Only PNG, JPG, JPEG, and PDF files are permitted.
* Individual image files cannot exceed 3 MB.
* Individual PDF files cannot exceed 5 MB.
* Total attachment size cannot exceed 15 MB.
* Files are stored in complaint-specific folders.
* Each uploaded file is assigned a unique GUID-based filename.
* Empty attachment folders are automatically removed during cleanup.
* All attachment uploads are linked to the currently authenticated employee.
## START PROGRESS OF A COMPLAINT

### Purpose

Allows the currently assigned handler to begin working on a complaint by changing its status from **ASSIGNED** to **IN_PROGRESS**.

---

### Validation Checks

#### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.
* If the complaint does not exist, throw a **Not Found Exception**.

---

#### Current Handler Validation

* Verify that the logged-in employee is the current handler of the complaint.
* Only the assigned handler is authorized to start progress.

**Failure Condition**

* If the logged-in employee is not the current handler, throw a **Forbidden Exception**.

---

#### Status Validation

* Verify that the complaint is currently in **ASSIGNED** status.

**Failure Condition**

* If the complaint is in any other status, throw a **Business Rule Exception**.

---

### Status Transition

#### Allowed Transition

```text
ASSIGNED → IN_PROGRESS
```

---

### Update Complaint

1. Update complaint status to **IN_PROGRESS**.
2. Set the UpdatedAt timestamp to the current UTC time.
3. Save the updated complaint record.

---

### Create Complaint History

Insert a record into the **Complaint_History** table containing:

* Complaint Id
* Previous Status = ASSIGNED
* New Status = IN_PROGRESS
* Previous Handler
* Current Handler
* Action Performed By
* User Role
* Remarks:

  * "Complaint resolution progress started."
* Escalation Level
* Timestamp

### Business Rules

* Only the current assigned handler can start complaint progress.
* Only complaints in **ASSIGNED** status can be moved to **IN_PROGRESS**.
* Every status change must be recorded in the complaint history.
* The operation is executed within a database transaction to ensure consistency.

---

### Status Flow

```text
SUBMITTED
    ↓
ASSIGNED
    ↓
IN_PROGRESS
```
## RESOLVE A COMPLAINT

### Purpose

Allows the current complaint handler to mark a complaint as **RESOLVED** after completing the required resolution activities.

---

### Validation Checks

#### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.
* If the complaint does not exist, throw a **Not Found Exception**.

---

#### Current Handler Validation

* Verify that the logged-in employee is the current handler of the complaint.
* Only the assigned handler can resolve the complaint.

**Failure Condition**

* If the logged-in employee is not the current handler, throw a **Forbidden Exception**.

---

#### Status Validation

* Verify that the complaint is currently in **IN_PROGRESS** status.

**Failure Condition**

* If the complaint is not in **IN_PROGRESS** status, throw a **Business Rule Exception**.

---

### Status Transition

#### Allowed Transition

```text
IN_PROGRESS → RESOLVED
```

---

### Update Complaint

1. Update complaint status to **RESOLVED**.
2. Set the **ResolvedAt** timestamp to the current UTC time.
3. Set the **UpdatedAt** timestamp to the current UTC time.
4. Save the updated complaint record.

---

### Create Complaint History

Insert a record into the **Complaint_History** table containing:

* Complaint Id
* Previous Status = IN_PROGRESS
* New Status = RESOLVED
* Previous Handler
* Current Handler
* Action Performed By
* User Role
* Resolution Remarks
* Escalation Level
* Timestamp

The resolution remarks provided by the handler are stored as part of the complaint history.

---

### Send Notification

#### Notify Complaint Creator

After successful resolution:

* Send a notification to the employee who raised the complaint.

**Notification Details**

| Field     | Value              |
| --------- | ------------------ |
| Type      | Complaint Resolved |
| Title     | Complaint Resolved |
| Recipient | Complaint Creator  |
| Reference | Complaint Id       |

**Message**

```text
Your complaint '{Complaint Title}' has been resolved.
```

### Business Rules

* Only the current assigned handler can resolve a complaint.
* A complaint can only be resolved when it is in **IN_PROGRESS** status.
* Resolution remarks must be recorded in complaint history.
* The complaint creator must be notified after resolution.
* Every status change must be recorded in complaint history.
* The operation is executed within a database transaction to ensure data consistency.

---

### Status Flow

```text
ASSIGNED
    ↓
IN_PROGRESS
    ↓
RESOLVED
```
## CLOSE A COMPLAINT

### Purpose

Allows the complaint creator to formally close a complaint after verifying that the provided resolution is satisfactory.

---

### Validation Checks

#### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.
* If the complaint does not exist, throw a **Not Found Exception**.

---

#### Complaint Creator Validation

* Verify that the logged-in employee is the creator of the complaint.
* Only the complaint creator is authorized to close the complaint.

**Failure Condition**

* If the logged-in employee is not the complaint creator, throw a **Forbidden Exception**.

---

#### Status Validation

* Verify that the complaint is currently in **RESOLVED** status.

**Failure Condition**

* If the complaint is not in **RESOLVED** status, throw a **Business Rule Exception**.

---

### Status Transition

#### Allowed Transition

```text
RESOLVED → CLOSED
```

---

### Update Complaint

1. Update complaint status to **CLOSED**.
2. Set the **ClosedAt** timestamp to the current UTC time.
3. Set the **UpdatedAt** timestamp to the current UTC time.
4. Save the updated complaint record.

---

### Create Complaint History

Insert a record into the **Complaint_History** table containing:

* Complaint Id
* Previous Status = RESOLVED
* New Status = CLOSED
* Previous Handler
* Current Handler
* Action Performed By
* User Role
* Closing Remarks
* Escalation Level
* Timestamp

The remarks provided by the complaint creator are stored as part of the complaint history.

---

### Send Notification

#### Notify Current Handler

After successful closure:

* Send a notification to the complaint's current handler.

**Notification Details**

| Field     | Value            |
| --------- | ---------------- |
| Type      | Complaint Closed |
| Title     | Complaint Closed |
| Recipient | Current Handler  |
| Reference | Complaint Id     |

**Message**

```text
Complaint '{Complaint Title}' has been closed by the requester.
```

---

### Audit Logging

Record an application log entry indicating:

* Complaint Id
* Employee Id of the complaint creator
* Complaint closure action performed

### Business Rules

* Only the complaint creator can close a complaint.
* A complaint can only be closed when it is in **RESOLVED** status.
* Closing remarks must be recorded in complaint history.
* The current handler must be notified when the complaint is closed.
* Every status change must be recorded in complaint history.
* The operation is executed within a database transaction to ensure data consistency.

---

### Status Flow

```text
IN_PROGRESS
    ↓
RESOLVED
    ↓
CLOSED
```
## REOPEN A COMPLAINT

### Purpose

Allows the complaint creator to reopen a complaint when the provided resolution is unsatisfactory or when the issue persists after resolution or external escalation.

---

### Validation Checks

#### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.
* If the complaint does not exist, throw a **Not Found Exception**.

---

#### Complaint Creator Validation

* Verify that the logged-in employee is the creator of the complaint.
* Only the complaint creator is authorized to reopen a complaint.

**Failure Condition**

* If the logged-in employee is not the complaint creator, throw a **Forbidden Exception**.

---

#### Status Validation

A complaint can only be reopened if its current status is:

* **RESOLVED**
* **EXTERNALLY_ESCALATED**

**Failure Condition**

* If the complaint is in any other status, throw a **Business Rule Exception**.

---

#### Reopen Limit Validation

* Verify the complaint's reopen count.
* A complaint can be reopened a maximum of **3 times**.

**Failure Condition**

* If the reopen count has reached 3, throw a **Business Rule Exception**.

**Message**

```text
Cannot reopen a complaint more than 3 times. Please raise a new complaint.
```

---

### Status Transition

#### Initial Reopen Transition

```text
RESOLVED → REOPENED

OR

EXTERNALLY_ESCALATED → REOPENED
```

---

### Update Complaint

1. Update complaint status to **REOPENED**.
2. Increment **ReopenedCount** by 1.
3. Clear the **ResolvedAt** timestamp.
4. Clear the **ClosedAt** timestamp.
5. Update the **UpdatedAt** timestamp.
6. Save the complaint.

---

### Create Reopen History

Insert a record into the **Complaint_History** table containing:

* Complaint Id
* Previous Status
* New Status = REOPENED
* Previous Handler
* Current Handler
* Action Performed By
* User Role
* Reopen Remarks
* Escalation Level
* Timestamp

---

### Determine New Assignment

After reopening, the complaint is reassigned using the standard complaint assignment algorithm.

#### Assignment Process

1. Execute the Complaint Assignment Engine.
2. Determine:

   * New Handler
   * Escalation Level

The assignment follows the configured assignment hierarchy:

```text
GRO
 ↓
Department Head
 ↓
Admin
```

---

### Create Assignment History

Insert a record into the **Complaint_Assignment_History** table containing:

* Complaint Id
* Previous Handler
* New Handler
* Assignment Reason = REOPEN
* Assignment Timestamp

---

### Reassign Complaint

1. Update Current Handler.
2. Update Escalation Level.
3. Recalculate Escalation Due Date.
4. Change status from **REOPENED** to **ASSIGNED**.
5. Update UpdatedAt timestamp.
6. Save the complaint.

---

### Create Reassignment History

Insert another complaint history record containing:

* Previous Status = REOPENED
* New Status = ASSIGNED
* Previous Handler
* New Handler
* Remarks:

  * "Complaint reassigned after reopen."
* Escalation Level
* Timestamp

---

### Send Notifications

#### Notify New Handler

Send a notification to the newly assigned handler.

**Notification Details**

| Field     | Value                  |
| --------- | ---------------------- |
| Type      | Complaint Reopened     |
| Title     | Complaint Reopened     |
| Recipient | Newly Assigned Handler |
| Reference | Complaint Id           |

**Message**

```text
Complaint '{Complaint Title}' has been reopened and assigned to you.
```

---

#### Notify Complaint Creator

Send a notification to the complaint creator confirming the reopen action.

**Notification Details**

| Field     | Value              |
| --------- | ------------------ |
| Type      | Complaint Reopened |
| Title     | Complaint Reopened |
| Recipient | Complaint Creator  |
| Reference | Complaint Id       |

**Message**

```text
Your complaint '{Complaint Title}' has been reopened.
```

---

### Audit Logging

Record an application log entry indicating:

* Complaint Id
* Employee Id of the complaint creator
* Reopen action performed

### Business Rules

* Only the complaint creator can reopen a complaint.
* A complaint can only be reopened from **RESOLVED** or **EXTERNALLY_ESCALATED** status.
* A complaint can be reopened a maximum of **3 times**.
* Reopen remarks must be recorded in complaint history.
* Every reopen action must create a complaint history record.
* Every reassignment must create an assignment history record.
* Reopened complaints are reassigned through the standard assignment engine.
* Escalation level and escalation due date are recalculated after reassignment.
* Notifications must be sent to both the new handler and the complaint creator.
* The operation is executed within a database transaction to ensure consistency.

---

### Status Flow

```text
RESOLVED
      ↓
REOPENED
      ↓
ASSIGNED
      ↓
IN_PROGRESS

OR

EXTERNALLY_ESCALATED
          ↓
REOPENED
          ↓
ASSIGNED
```
## ESCALATE A COMPLAINT

### Purpose

Allows the current handler of a complaint to manually escalate it to the next authority level when it cannot be resolved at the current level.

---

### Validation Checks

#### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.
* If the complaint does not exist, throw a **Not Found Exception**.

---

#### Current Handler Validation

* Verify that the logged-in employee is the current handler of the complaint.
* Only the assigned handler is authorized to escalate the complaint.

**Failure Condition**

* If the logged-in employee is not the current handler, throw a **Forbidden Exception**.

---

#### Status Validation

* Verify that the complaint is currently in **IN_PROGRESS** status.

**Failure Condition**

* If the complaint is not in **IN_PROGRESS**, throw a **Business Rule Exception**.

---

### Escalation Eligibility Rules

#### Role-Based Escalation

Escalation is strictly controlled based on the role of the current handler.

---

##### GRO → Department Head

* If the current role is **GRO**

  * Fetch Department Head for the complaint’s department.
  * Assign complaint to Department Head.
  * Set **Escalation Level = 1**.

**Failure Condition**

* If Department Head is not found → throw Business Rule Exception.

---

##### Department Head → Admin

* If the current role is **Department Head**

  * Fetch Admin user.
  * Assign complaint to Admin.
  * Set **Escalation Level = 2**.

**Failure Condition**

* If Admin is not found → throw Business Rule Exception.

---

##### Admin Escalation

* Admin is not allowed to escalate complaints.

**Failure Condition**

* Throw Business Rule Exception:

  * `"Admin cannot escalate complaints."`

---

### Status Transition

#### Allowed Transition

```text id="esc_flow_1"
IN_PROGRESS → ESCALATED
```

---

### Update Complaint

1. Update **Current Handler** to the new handler.
2. Update **Status** to **ESCALATED**.
3. Update **Escalation Level** based on new authority.
4. Recalculate **Escalation Due At** using the escalation engine:

   * Based on category, priority, and new escalation level.
5. Update **UpdatedAt** timestamp.
6. Save complaint changes.

---

### Create Escalation Record

Insert a record into **Complaint_Escalation** table containing:

* Complaint Id
* Escalated To Employee Id
* Escalated By Employee Id
* Escalation Level
* Reason (from DTO remarks)
* Escalation Timestamp

---

### Create Assignment History

Insert a record into **Complaint_Assignment_History** containing:

* Complaint Id
* Old Handler
* New Handler
* Assigned By (current user)
* Assignment Reason = ESCALATION
* Timestamp

---

### Create Complaint History

Insert a record into **Complaint_History** containing:

* Complaint Id
* Previous Status = IN_PROGRESS
* New Status = ESCALATED
* Previous Handler
* New Handler
* Action Performed By
* User Role
* Escalation Remarks
* Escalation Level
* Timestamp

---

### Send Notifications

#### Notify New Handler

* Send notification to the newly escalated handler.

**Details**

| Field     | Value               |
| --------- | ------------------- |
| Type      | Complaint Escalated |
| Title     | Complaint Escalated |
| Recipient | New Handler         |
| Reference | Complaint Id        |

**Message**

```text id="msg1"
Complaint '{Complaint Title}' has been escalated to you.
```

---

#### Notify Complaint Creator

* Send notification to the complaint creator informing escalation.

**Details**

| Field     | Value               |
| --------- | ------------------- |
| Type      | Complaint Escalated |
| Title     | Complaint Escalated |
| Recipient | Complaint Creator   |
| Reference | Complaint Id        |

**Message**

```text id="msg2"
Your complaint '{Complaint Title}' has been escalated.
```

### Business Rules

* Only the current handler can escalate a complaint.
* Only complaints in **IN_PROGRESS** status can be escalated.
* GRO can escalate only to Department Head.
* Department Head can escalate only to Admin.
* Admin cannot escalate complaints.
* Every escalation must create:

  * Escalation record
  * Assignment history record
  * Complaint history record
* Escalation updates handler and recalculates due date.
* Notifications must be sent to both new handler and complaint creator.
* The operation is executed within a database transaction to ensure consistency.

---

### Status Flow

```text id="flow_esc_2"
IN_PROGRESS → ESCALATED → (Higher Authority Handling)
```

## MANUALLY ASSIGN A COMPLAINT TO A GRO

### Purpose

Allows an **Admin** to manually assign or reassign a complaint to an eligible **GRO (Grievance Redressal Officer)** within the same department.

---

### Validation Checks

#### Complaint Existence Validation

* Retrieve the complaint using the provided Complaint Id.
* If the complaint does not exist, throw a **Not Found Exception**.

---

#### Admin Authorization Validation

* Verify that the logged-in user has the **Admin** role.
* Only Admins are authorized to manually assign complaints.

**Failure Condition**

* If the logged-in user is not an Admin, throw a **Forbidden Exception**.

---

#### Complaint Status Validation

Manual assignment is not allowed when the complaint has already reached a final state.

**Restricted Statuses**

* RESOLVED
* REJECTED
* CLOSED
* EXTERNALLY_ESCALATED

**Failure Condition**

* If the complaint is in any of the above statuses, throw a **Business Rule Exception**.

**Message**

```text
Complaint cannot be manually assigned in its current status.
```

---

### GRO Validation

#### Employee Existence Validation

* Retrieve the employee using the provided GRO Employee Id.

**Failure Condition**

* If the employee does not exist, throw a **Not Found Exception**.

---

#### Employee Active Validation

* Verify that the selected employee is active.

**Failure Condition**

* If the employee is inactive, throw a **Business Rule Exception**.

**Message**

```text
Employee is inactive.
```

---

#### Role Validation

* Verify that the employee's role is **GRO**.

**Failure Condition**

* If the employee is not a GRO, throw a **Business Rule Exception**.

**Message**

```text
Complaint can only be assigned to a GRO.
```

---

#### Department Validation

* Verify that the selected GRO belongs to the same department as the complaint category.

**Failure Condition**

* If the GRO belongs to a different department, throw a **Business Rule Exception**.

**Message**

```text
Complaint can only be assigned to an employee in the same department.
```

---

#### Previous Handler Validation

* Retrieve all previous handlers of the complaint.
* Verify that the selected GRO has never handled the complaint before.

**Failure Condition**

* If the GRO already handled the complaint previously, throw a **Business Rule Exception**.

**Message**

```text
Cannot assign to a handler who has already handled this complaint.
```

---

### Status Transition

#### Allowed Transition

The complaint can be manually assigned from any non-final status.

```text
ANY ACTIVE STATUS → ASSIGNED
```

---

### Update Complaint

1. Store the current handler as **Old Handler**.
2. Store the current complaint status as **Old Status**.
3. Update **Current Handler** to the selected GRO.
4. Reset **Escalation Level = 0**.
5. Recalculate **Escalation Due At** using the assignment engine.
6. Update **Status = ASSIGNED**.
7. Update **UpdatedAt** timestamp.
8. Save complaint changes.

---

### Create Assignment History

Insert a record into **Complaint_Assignment_History** containing:

* Complaint Id
* Old Handler Employee Id
* New Handler Employee Id
* Assigned By (Current Admin)
* Assignment Reason = MANUAL
* Timestamp

---

### Create Complaint History

Insert a record into **Complaint_History** containing:

* Complaint Id
* Previous Status
* New Status = ASSIGNED
* Previous Handler
* New Handler
* Action Performed By
* User Role
* Remarks (from DTO)
* Escalation Level = 0
* Timestamp

---

### Send Notifications

#### Notify Newly Assigned GRO

* Send notification to the newly assigned GRO.

**Details**

| Field     | Value              |
| --------- | ------------------ |
| Type      | Complaint Assigned |
| Title     | Complaint Assigned |
| Recipient | Assigned GRO       |
| Reference | Complaint Id       |

**Message**

```text
Complaint '{Complaint Title}' has been assigned to you.
```

---

#### Notify Complaint Creator

* Send notification to the employee who raised the complaint.

**Details**

| Field     | Value                |
| --------- | -------------------- |
| Type      | Complaint Assigned   |
| Title     | Complaint Reassigned |
| Recipient | Complaint Creator    |
| Reference | Complaint Id         |

**Message**

```text
Your complaint '{Complaint Title}' has been reassigned.
```

---

### Business Rules

* Only Admins can manually assign complaints.
* Complaints in final statuses cannot be reassigned.
* Complaint can only be assigned to a GRO.
* GRO must be active.
* GRO must belong to the same department as the complaint category.
* Complaint cannot be assigned to a previous handler.
* Manual assignment resets escalation level to **0**.
* Escalation due date must be recalculated.
* Every manual assignment must create:

  * Assignment history record
  * Complaint history record
* Notifications must be sent to:

  * Newly assigned GRO
  * Complaint creator
* The operation must execute within a database transaction to ensure consistency.

---

### Status Flow

```text
OPEN / ASSIGNED / IN_PROGRESS / ESCALATED
                    ↓
                ASSIGNED
                    ↓
            Assigned GRO Handling
```



### Audit Logging

After successful completion:

```text
Complaint {ComplaintId} assigned to Employee {GroEmployeeId}
```

is recorded in the application logs for auditing and tracking purposes.
