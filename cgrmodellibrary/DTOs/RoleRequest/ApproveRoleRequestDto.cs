public class ApproveRoleRequestDto
{
    // Nullable because employee may already belong
    // to a department.
    // Required only when employee currently
    // has no department assigned.
    public int? AssignDepartmentId { get; set; }

    // Nullable because approval remarks
    // are optional.
    public string? Remarks { get; set; }
}