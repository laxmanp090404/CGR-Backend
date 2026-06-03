using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class NotificationRepository : AbstractRepository<int, Notification>, INotificationRepository
{
    private readonly CGRContext _context;

    public NotificationRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetByEmployeeIdAsync(int employeeId, bool? isRead, int page, int pageSize)
    {
        var query = _context.Notifications
            .Include(n => n.NotificationType)
            .Where(n => n.EmployeeId == employeeId)
            .AsQueryable();

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(int employeeId)
    {
        return await _context.Notifications
            .CountAsync(n => n.EmployeeId == employeeId && n.IsRead == false);
    }

    public async Task MarkAllAsReadAsync(int employeeId)
    {
        var unread = await _context.Notifications
            .Where(n => n.EmployeeId == employeeId && n.IsRead == false)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }
}
