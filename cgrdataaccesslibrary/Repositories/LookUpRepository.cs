using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly CGRContext _context;

    public LookupRepository(CGRContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Role>> GetRolesAsync()
    {
        return await _context.Roles.OrderBy(r => r.RoleId).ToListAsync();
    }

    public async Task<IEnumerable<Priority>> GetPrioritiesAsync()
    {
        return await _context.Priorities.OrderBy(p => p.EscalationOrder).ToListAsync();
    }

    public async Task<IEnumerable<ComplaintStatus>> GetComplaintStatusesAsync()
    {
        return await _context.ComplaintStatuses.OrderBy(cs => cs.StatusId).ToListAsync();
    }

    public async Task<IEnumerable<RequestType>> GetRequestTypesAsync()
    {
        return await _context.RequestTypes.OrderBy(rt => rt.RequestTypeId).ToListAsync();
    }

    public async Task<IEnumerable<NotificationType>> GetNotificationTypesAsync()
    {
        return await _context.NotificationTypes.OrderBy(nt => nt.NotificationTypeId).ToListAsync();
    }

    public async Task<IEnumerable<RequestStatus>> GetRequestStatusesAsync()
    {
        return await _context.RequestStatuses.OrderBy(rs => rs.RequestStatusId).ToListAsync();
    }
}
