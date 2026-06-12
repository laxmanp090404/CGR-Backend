using cgrmodellibrary.DTOs.Analytics;
using cgrmodellibrary.DTOs.Common;

namespace cgrdataaccesslibrary.Interfaces;

public interface IAnalyticsRepository
{
    Task<AdminDashboardDto> GetAdminDashboardAsync();

    Task<MyDashboardDto> GetMyDashboardAsync(int employeeId);

    Task<GroDashboardDto> GetGroDashboardAsync(int employeeId);

    Task<IEnumerable<DepartmentDashboardDto>> GetDepartmentDashboardsAsync(int? departmentId,bool isAdmin);

    Task<IEnumerable<StatusDistributionDto>> GetStatusDistributionAsync(short roleId,int employeeId,int? departmentId);

    Task<IEnumerable<TopCategoryDto>>GetTopCategoriesAsync(short roleId,int? departmentId,int n);
    Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync(short roleId,int? departmentId);
}