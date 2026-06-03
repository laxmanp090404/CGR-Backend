using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class AuditLogRepository : AbstractRepository<int, AuditLog>, IAuditLogRepository
{
    private readonly CGRContext _context;

    public AuditLogRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? tableName, long? recordId, int? changedBy, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs
            .Include(a => a.ChangedByNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tableName))
        {
            query = query.Where(a => a.TableName == tableName);
        }
        if (recordId.HasValue)
        {
            query = query.Where(a => a.RecordId == recordId.Value);
        }
        if (changedBy.HasValue)
        {
            query = query.Where(a => a.ChangedBy == changedBy.Value);
        }
        if (fromDate.HasValue)
        {
            query = query.Where(a => a.ChangedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            query = query.Where(a => a.ChangedAt <= toDate.Value);
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
