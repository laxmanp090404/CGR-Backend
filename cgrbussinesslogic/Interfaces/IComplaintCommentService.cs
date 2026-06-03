using cgrmodellibrary.DTOs.Comment;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintCommentService
{
    Task<IEnumerable<ComplaintCommentDto>> GetByComplaintIdAsync(int complaintId, int currentEmployeeId, string role);
    Task<ComplaintCommentDto> AddCommentAsync(int complaintId, CreateComplaintCommentDto dto, int employeeId, string role);
}
