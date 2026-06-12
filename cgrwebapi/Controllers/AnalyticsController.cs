using cgrbussinesslogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(
        IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

   
    [HttpGet("admin-dashboard")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var result =
            await _analyticsService
                .GetAdminDashboardAsync();

        return Ok(result);
    }

   
    [HttpGet("my-dashboard")]
    public async Task<IActionResult> GetMyDashboard()
    {
        var result =
            await _analyticsService
                .GetMyDashboardAsync();

        return Ok(result);
    }

    
    [HttpGet("gro-dashboard")]
    [Authorize(Policy = "GROOrAdmin")]
    public async Task<IActionResult> GetGroDashboard()
    {
        var result =
            await _analyticsService
                .GetGroDashboardAsync();

        return Ok(result);
    }

    
    [HttpGet("department-dashboard")]
    [Authorize(Policy = "DeptHeadOrAdmin")]
    public async Task<IActionResult> GetDepartmentDashboard()
    {
        var result =
            await _analyticsService
                .GetDepartmentDashboardAsync();

        return Ok(result);
    }

    [HttpGet("status-distribution")]
    public async Task<IActionResult> GetStatusDistribution()
    {
        var result =
            await _analyticsService
                .GetStatusDistributionAsync();

        return Ok(result);
    }

    
    [HttpGet("top-categories")]
    [Authorize(Policy = "DeptHeadOrAdmin")]
    public async Task<IActionResult> GetTopCategories(
        [FromQuery] int n = 5)
    {
        if (n <= 0)
        {
            n = 5;
        }

        var result =
            await _analyticsService
                .GetTopCategoriesAsync(n);

        return Ok(result);
    }

[HttpGet("complaint-summary")]
[Authorize(Policy = "DeptHeadOrAdmin")]
public async Task<IActionResult> GetComplaintSummary()
{
    var result = await _analyticsService.GetComplaintAnalyticsAsync();

    return Ok(result);
}
}