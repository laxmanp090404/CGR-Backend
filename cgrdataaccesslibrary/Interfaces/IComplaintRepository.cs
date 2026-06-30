using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintRepository : IRepository<int, Complaint>
{
    Task<Complaint?> GetDetailByIdAsync(int id);
    Task<(IEnumerable<VComplaintDashboard> Items, int TotalCount)> GetPagedDashboardAsync(
        int page, int pageSize, int? statusId, int? priorityId, int? categoryId, int? departmentId, string? search,
        int employeeIdFilter, string roleFilter, int? deptIdFilter, bool? raisedbyme, string? sortBy = null);
     Task<IEnumerable<Complaint>> GetEscalationDueComplaintsAsync();
     // For duplicate complaint check
     Task<bool> ExistsRecentDuplicateAsync(int raisedByEmployeeId,string title,string description,TimeSpan window);
     Task<(IEnumerable<Complaint> Items, int TotalCount)> GetMyWorkQueueAsync(
         int page,
         int pageSize,
         int employeeId,
         int? statusId = null,
         int? priorityId = null,
         int? categoryId = null,
         int? departmentId = null,
         string? search = null,
         string? sortBy = null);
}
