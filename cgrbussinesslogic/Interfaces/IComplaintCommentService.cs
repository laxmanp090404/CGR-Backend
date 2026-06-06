using cgrmodellibrary.DTOs.Comment;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintCommentService
{
    Task<IEnumerable<ComplaintCommentDto>>GetByComplaintIdAsync(int complaintId);

    Task<ComplaintCommentDto>AddCommentAsync(int complaintId,CreateComplaintCommentDto dto);
}
