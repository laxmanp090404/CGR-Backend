using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface INotificationRepository : IRepository<int, Notification>
{
    Task<(IEnumerable<Notification> Items, int TotalCount)> GetByEmployeeIdAsync(int employeeId, bool? isRead, int page, int pageSize);
    Task<int> GetUnreadCountAsync(int employeeId);
    Task MarkAllAsReadAsync(int employeeId);
}
