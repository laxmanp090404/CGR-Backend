using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Analytics;
using cgrmodellibrary.Exceptions;

namespace cgrbussinesslogic.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ICurrentUserService _currentUserService;
    private const short ROLE_ADMIN = 4;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_GRO = 2;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        ICurrentUserService currentUserService)
    {
        _analyticsRepository = analyticsRepository;
        _currentUserService = currentUserService;
    }

    public Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
      if(_currentUserService.RoleId != ROLE_ADMIN)
        {
            throw new ForbiddenException("Only Admin can access the Admin dashboard.");
        }
      return _analyticsRepository.GetAdminDashboardAsync();
        
    }

    public Task<MyDashboardDto> GetMyDashboardAsync()
    {
      if(_currentUserService.RoleId == ROLE_ADMIN)
        {
            throw new ForbiddenException("Admin does not have a personal Complaint dashboard.");
        }  
     return _analyticsRepository.GetMyDashboardAsync(_currentUserService.EmployeeId);
    }
    public Task<GroDashboardDto> GetGroDashboardAsync()
    {
        if(_currentUserService.RoleId != ROLE_GRO)
        {
            throw new ForbiddenException("Only GRO can access the GRO dashboard.");
        }
         return _analyticsRepository.GetGroDashboardAsync(
            _currentUserService.EmployeeId);
    }
       

    public async Task<IEnumerable<DepartmentDashboardDto>> GetDepartmentDashboardAsync()
    {
        if(_currentUserService.RoleId != ROLE_ADMIN && _currentUserService.RoleId != ROLE_DEPARTMENT_HEAD)
        {
            throw new ForbiddenException("Only Admin or Department Head can access department dashboards.");
        }
        return await _analyticsRepository
            .GetDepartmentDashboardsAsync(
                _currentUserService.DepartmentId,
                _currentUserService.RoleId ==
                ROLE_ADMIN);
    }

    // employee-complaints raised  ,gro-complaints handled,depth head - whole dept ,admin-all
    public async Task<IEnumerable<StatusDistributionDto>> GetStatusDistributionAsync()
    {
        return await _analyticsRepository.GetStatusDistributionAsync(
                _currentUserService.RoleId,
                _currentUserService.EmployeeId,
                _currentUserService.DepartmentId);
    }

    // admin - all categories,depthead-his dept
    public async Task<IEnumerable<TopCategoryDto>> GetTopCategoriesAsync(int n = 5)
    {
        if (_currentUserService.RoleId!= ROLE_ADMIN && _currentUserService.RoleId != ROLE_DEPARTMENT_HEAD)
        {
            throw new ForbiddenException("Only Admin or Department Head can access top categories.");
        }

        return await _analyticsRepository.GetTopCategoriesAsync(_currentUserService.RoleId,_currentUserService.DepartmentId,n);
    }

    public async Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync()
{
    if (_currentUserService.RoleId != ROLE_ADMIN &&
        _currentUserService.RoleId != ROLE_DEPARTMENT_HEAD)
    {
        throw new ForbiddenException("Only Admin or Department Head can access complaint analytics.");
    }

    return await _analyticsRepository.GetComplaintAnalyticsAsync(
            _currentUserService.RoleId,
            _currentUserService.DepartmentId);
}
}