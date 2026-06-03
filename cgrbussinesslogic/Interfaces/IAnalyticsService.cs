using cgrmodellibrary.DTOs.Analytics;

namespace cgrbussinesslogic.Interfaces;

public interface IAnalyticsService
{
    Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync(int? departmentId, DateTime? fromDate, DateTime? toDate);
}
