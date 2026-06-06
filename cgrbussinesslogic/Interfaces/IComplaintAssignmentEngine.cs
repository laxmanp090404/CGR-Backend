using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintAssignmentEngine
{
    public Task<(int HandlerId, short EscalationLevel)>DetermineInitialAssignmentAsync(int complaintId, int categoryDepartmentId, short creatorRole);
    public Task<DateTime?> CalculateEscalationDueAtAsync(Complaint complaint);

}