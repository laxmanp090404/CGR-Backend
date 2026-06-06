namespace cgrbussinesslogic.Interfaces;

using cgrmodellibrary.DTOs.Attachment;
using cgrmodellibrary.Models;
using Microsoft.AspNetCore.Http;



public interface IComplaintAttachmentService
{
    Task<IEnumerable<ComplaintAttachmentDto>> GetByComplaintIdAsync(int complaintId);

    Task<(List<ComplaintAttachment> Attachments, List<string> CreatedFiles)> SaveAttachmentsAsync(int complaintId, List<IFormFile> files);
    Task DeleteFilesAsync(IEnumerable<string> filePaths);
}