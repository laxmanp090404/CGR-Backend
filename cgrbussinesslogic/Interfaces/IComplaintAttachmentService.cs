using cgrmodellibrary.DTOs.Attachment;
using Microsoft.AspNetCore.Http;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintAttachmentService
{
    Task<IEnumerable<ComplaintAttachmentDto>> GetByComplaintIdAsync(int complaintId);
    Task<ComplaintAttachmentDto> UploadAsync(int complaintId, IFormFile file, int employeeId);
    Task DeleteAsync(int attachmentId, int currentEmployeeId, string role);
}
