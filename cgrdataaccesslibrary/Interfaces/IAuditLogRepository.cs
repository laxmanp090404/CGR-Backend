using cgrmodellibrary.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IAuditLogRepository : IRepository<int, AuditLog>
{
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? tableName, long? recordId, int? changedBy, DateTime? fromDate, DateTime? toDate);
}
