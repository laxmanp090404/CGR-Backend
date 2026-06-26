using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Notification;
using cgrwebapi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace cgrwebapi.Infrastructure;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushAsync(int employeeId, NotificationDto notificationDto)
    {
        await _hubContext.Clients.User(employeeId.ToString()).SendAsync("ReceiveNotification", notificationDto);
    }
}
