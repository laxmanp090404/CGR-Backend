using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintAssignmentRepository : IRepository<int, ComplaintAssignmentHistory>
{   // get prev handlers of a complaint
    Task<List<int>> GetPreviousHandlersAsync(int complaintId);
    //get assignment history of a complaint
    Task<IEnumerable<ComplaintAssignmentHistory>> GetByComplaintIdAsync(int complaintId);
}
