using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintAttachmentRepository : IRepository<int, ComplaintAttachment>
{
    Task<IEnumerable<ComplaintAttachment>> GetByComplaintIdAsync(int complaintId);
    Task<ComplaintAttachment?> GetByFilePathAsync(string filePath);
}
