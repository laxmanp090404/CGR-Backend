using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LookUpController{
    private readonly ILookUpService _lookUpService;
    public LookUpController(ILookUpService lookUpService)
    {
        _lookUpService = lookUpService;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<Role>>> GetRolesAsync()
    {
        var roles = await _lookUpService.GetRolesAsync();
        return new OkObjectResult(roles);
    }  
    [HttpGet("priorities")] 
    public async Task<ActionResult<IEnumerable<Priority>>> GetPrioritiesAsync()
    {
        var priorities = await _lookUpService.GetPrioritiesAsync();
        return new OkObjectResult(priorities);
    }
    [HttpGet("complaint-statuses")]
    public async Task<ActionResult<IEnumerable<ComplaintStatus>>> GetComplaintStatusesAsync()
    {
        var complaintStatuses = await _lookUpService.GetComplaintStatusesAsync();
        return new OkObjectResult(complaintStatuses);
    }
    [HttpGet("request-types")]
    public async Task<ActionResult<IEnumerable<RequestType>>> GetRequestTypesAsync()
    {
        var requestTypes = await _lookUpService.GetRequestTypesAsync();
        return new OkObjectResult(requestTypes);
    }
    [HttpGet("notification-types")]
    public async Task<ActionResult<IEnumerable<NotificationType>>> GetNotificationTypesAsync()
    {
        var notificationTypes = await _lookUpService.GetNotificationTypesAsync();
        return new OkObjectResult(notificationTypes);   
    }
    [HttpGet("request-statuses")]
    public async Task<ActionResult<IEnumerable<RequestStatus>>> GetRequestStatusesAsync()
    {
        var requestStatuses = await _lookUpService.GetRequestStatusesAsync();
        return new OkObjectResult(requestStatuses);
    }
}