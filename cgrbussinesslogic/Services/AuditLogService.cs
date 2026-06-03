using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.AuditLog;
using cgrmodellibrary.DTOs.Common;

namespace cgrbussinesslogic.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedResultDto<AuditLogDto>> GetPagedAsync(
        int page, int pageSize, string? tableName, long? recordId, int? changedBy, DateTime? fromDate, DateTime? toDate)
    {
        var (items, total) = await _auditLogRepository.GetPagedAsync(
            page, pageSize, tableName, recordId, changedBy, fromDate, toDate);

        return new PagedResultDto<AuditLogDto>
        {
            Items = items.Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                TableName = a.TableName,
                RecordId = a.RecordId,
                ColumnName = a.ColumnName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ChangedBy = a.ChangedBy,
                ChangedByName = a.ChangedByNavigation?.EmployeeName,
                ChangedAt = a.ChangedAt
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
