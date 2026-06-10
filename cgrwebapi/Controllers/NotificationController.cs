using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    // get all notifications
    [HttpGet]
    public async Task<ActionResult<
        PagedResultDto<NotificationDto>>> GetMyNotifications(
        [FromQuery] bool? isRead = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result =
            await _notificationService
                .GetMyNotificationsAsync(
                    _currentUserService.EmployeeId,
                    isRead,
                    page,
                    pageSize);

        return Ok(result);
    }
    // get unnread notification count
    [HttpGet("unread-count")]
public async Task<ActionResult<int>> GetUnreadCount()
{
    var count =
        await _notificationService
            .GetUnreadCountAsync(
                _currentUserService.EmployeeId);

    return Ok(count);
}
// mark all notification as read
[HttpPut("mark-all-read")]
public async Task<ActionResult> MarkAllRead()
{
    await _notificationService
        .MarkAllAsReadAsync(
            _currentUserService.EmployeeId);

    return NoContent();
}
[HttpPut("{notificationId:int}/read")]
public async Task<ActionResult> MarkAsRead(
    int notificationId)
{
    await _notificationService
        .MarkAsReadAsync(
            notificationId,
            _currentUserService.EmployeeId);

    return NoContent();
}

}