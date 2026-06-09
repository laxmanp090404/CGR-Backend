using cgrmodellibrary.DTOs.AuditLog;
using cgrmodellibrary.DTOs.Common;

namespace cgrbussinesslogic.Interfaces;

public interface IAuditLogService
{
    Task<PagedResultDto<AuditLogDto>> GetPagedAsync(int page, int pageSize, string? tableName, long? recordId, int? changedBy, DateTime? fromDate, DateTime? toDate);
}
