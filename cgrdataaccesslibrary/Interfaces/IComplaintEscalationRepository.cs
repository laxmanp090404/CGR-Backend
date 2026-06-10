using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
namespace cgrdataaccesslibrary.Interfaces;
public interface IComplaintEscalationRepository : IRepository<int, ComplaintEscalation>
{
    Task<IEnumerable<ComplaintEscalation>> GetByComplaintIdAsync(int complaintId);
}