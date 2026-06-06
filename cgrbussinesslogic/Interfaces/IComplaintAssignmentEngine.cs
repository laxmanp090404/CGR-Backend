using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintAssignmentEngine
{
    public Task<(int HandlerId, short EscalationLevel)>DetermineInitialAssignmentAsync(int complaintId, int categoryDepartmentId, short creatorRole);
    public Task<(int HandlerId, short EscalationLevel)>DetermineEscalationAssignmentAsync(Complaint complaint);
    public Task<DateTime?> CalculateEscalationDueAtAsync(Complaint complaint);

}