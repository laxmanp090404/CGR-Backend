using cgrmodellibrary.DTOs.Analytics;
using cgrmodellibrary.DTOs.Common;

namespace cgrbussinesslogic.Interfaces;
public interface IAnalyticsService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();

    Task<MyDashboardDto> GetMyDashboardAsync();

    Task<GroDashboardDto> GetGroDashboardAsync();

    Task<IEnumerable<DepartmentDashboardDto>> GetDepartmentDashboardAsync();

    Task<IEnumerable<StatusDistributionDto>> GetStatusDistributionAsync();

    Task<IEnumerable<TopCategoryDto>> GetTopCategoriesAsync(int n = 5);
    Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync();

}